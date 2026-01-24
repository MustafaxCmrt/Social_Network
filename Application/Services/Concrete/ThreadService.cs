using Application.DTOs.Thread;
using Application.DTOs.User;
using Application.DTOs.Category;
using Application.Services.Abstractions;
using Application.DTOs.Common;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Services;
using Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Concrete;

public class ThreadService : IThreadService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IModerationService _moderationService;

    public ThreadService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IModerationService moderationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _moderationService = moderationService;
    }

    public async Task<PagedResultDto<ThreadDto>> GetAllThreadsAsync(
        int page = 1,
        int pageSize = 20,
        string? q = null,
        int? categoryId = null,
        bool? isSolved = null,
        int? userId = null,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Include ile User ve Category bilgilerini yükle
        var threads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: query => query
                .Include(t => t.User)
                .Include(t => t.Category),
            cancellationToken);
        
        IEnumerable<Threads> query = threads;

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(t.Content) && t.Content.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (isSolved.HasValue)
        {
            query = query.Where(t => t.IsSolved == isSolved.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }

        // Sıralama
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "createdat" : sortBy.Trim();
        var normalizedSortDir = string.IsNullOrWhiteSpace(sortDir) ? "desc" : sortDir.Trim();
        var isAsc = string.Equals(normalizedSortDir, "asc", StringComparison.OrdinalIgnoreCase);

        query = normalizedSortBy.ToLowerInvariant() switch
        {
            "createdat" => isAsc ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt),
            "updatedat" => isAsc ? query.OrderBy(t => t.UpdatedAt) : query.OrderByDescending(t => t.UpdatedAt),
            "title" => isAsc ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
            "viewcount" => isAsc ? query.OrderBy(t => t.ViewCount) : query.OrderByDescending(t => t.ViewCount),
            _ => isAsc ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
        };

        // Extension metod ile sayfalandır
        return query.ToPagedResult(page, pageSize, MapToDto);
    }

    public async Task<ThreadDto?> GetThreadByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Include ile User ve Category bilgilerini yükle
        var threads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: query => query
                .Include(t => t.User)
                .Include(t => t.Category),
            cancellationToken);
        
        var thread = threads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
        {
            return null;
        }

        // ViewCount: detay sayfası görüntülenince artır
        thread.ViewCount++;
        _unitOfWork.Threads.Update(thread);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(thread);
    }

    public async Task<ThreadDto> CreateThreadAsync(CreateThreadDto createThreadDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(createThreadDto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Kategori ID: {createThreadDto.CategoryId} bulunamadı.");
        }

        // MODERASYON: Kullanıcı mute'lu mu kontrol et
        var (isMuted, activeMute) = await _moderationService.IsUserMutedAsync(currentUserId.Value);
        if (isMuted && activeMute != null)
        {
            throw new InvalidOperationException(
                $"Susturulduğunuz için konu açamazsınız. Bitiş: {activeMute.ExpiresAt:dd.MM.yyyy HH:mm}. Sebep: {activeMute.Reason}");
        }

        var thread = new Threads
        {
            Title = createThreadDto.Title,
            Content = createThreadDto.Content,
            CategoryId = createThreadDto.CategoryId,
            UserId = currentUserId.Value,
            ViewCount = 0,
            IsSolved = false
        };

        await _unitOfWork.Threads.CreateAsync(thread, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(thread);
    }

    public async Task<ThreadDto> UpdateThreadAsync(UpdateThreadDto updateThreadDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var currentRole = _currentUserService.GetCurrentUserRole();
        var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var thread = await _unitOfWork.Threads.GetByIdAsync(updateThreadDto.Id, cancellationToken);
        if (thread == null)
        {
            throw new KeyNotFoundException($"ID: {updateThreadDto.Id} olan konu bulunamadı.");
        }

        if (!isAdmin && thread.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Bu konuyu güncelleme yetkiniz yok.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(updateThreadDto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Kategori ID: {updateThreadDto.CategoryId} bulunamadı.");
        }

        thread.Title = updateThreadDto.Title;
        thread.Content = updateThreadDto.Content;
        thread.CategoryId = updateThreadDto.CategoryId;

        if (updateThreadDto.IsSolved.HasValue)
        {
            thread.IsSolved = updateThreadDto.IsSolved.Value;
        }

        _unitOfWork.Threads.Update(thread);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(thread);
    }

    public async Task<bool> DeleteThreadAsync(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var currentRole = _currentUserService.GetCurrentUserRole();
        var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var thread = await _unitOfWork.Threads.GetByIdAsync(id, cancellationToken);
        if (thread == null)
        {
            return false;
        }

        if (!isAdmin && thread.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Bu konuyu silme yetkiniz yok.");
        }

        var hasPosts = await _unitOfWork.Posts.AnyAsync(p => p.ThreadId == id, cancellationToken);
        if (hasPosts)
        {
            throw new InvalidOperationException("Bu konu silinemez çünkü altında cevaplar bulunmaktadır.");
        }

        _unitOfWork.Threads.Delete(thread);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ThreadDto MapToDto(Threads thread)
    {
        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = thread.Content,
            ViewCount = thread.ViewCount,
            IsSolved = thread.IsSolved,
            UserId = thread.UserId,
            CategoryId = thread.CategoryId,
            CreatedAt = thread.CreatedAt,
            UpdatedAt = thread.UpdatedAt,
            User = thread.User == null ? null : new UserSummaryDto
            {
                Id = thread.User.Id,
                FirstName = thread.User.FirstName,
                LastName = thread.User.LastName,
                Username = thread.User.Username,
                ProfileImg = thread.User.ProfileImg
            },
            Category = thread.Category == null ? null : new CategorySummaryDto
            {
                Id = thread.Category.Id,
                Title = thread.Category.Title,
                Slug = thread.Category.Slug
            }
        };
    }
}
