using Application.DTOs.AuditLog;
using Application.DTOs.Thread;
using Application.DTOs.User;
using Application.DTOs.Category;
using Application.Services.Abstractions;
using Application.DTOs.Common;

using Domain.Entities;
using Domain.Services;
using Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;

namespace Application.Services.Concrete;

public class ThreadService : IThreadService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IModerationService _moderationService;
    private readonly IMemoryCache _cache;
    private readonly IAuditLogService _auditLogService;

    public ThreadService(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService, 
        IModerationService moderationService,
        IMemoryCache cache,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _moderationService = moderationService;
        _cache = cache;
        _auditLogService = auditLogService;
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

        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "createdat" : sortBy.Trim();
        var normalizedSortDir = string.IsNullOrWhiteSpace(sortDir) ? "desc" : sortDir.Trim();
        var isAsc = string.Equals(normalizedSortDir, "asc", StringComparison.OrdinalIgnoreCase);

        var queryLower = string.IsNullOrWhiteSpace(q) ? null : q.Trim().ToLower();

        // ÖNEMLI: Kulüp thread'lerini gizle (ClubId == null olanlar normal forum thread'leri)
        Expression<Func<Threads, bool>>? predicate = t =>
            (queryLower == null
             || (t.Title != null && t.Title.ToLower().Contains(queryLower))
             || (t.Content != null && t.Content.ToLower().Contains(queryLower)))
            && (!categoryId.HasValue || t.CategoryId == categoryId.Value)
            && (!isSolved.HasValue || t.IsSolved == isSolved.Value)
            && (!userId.HasValue || t.UserId == userId.Value)
            && t.ClubId == null; // Sadece normal forum thread'leri (kulüp thread'leri hariç)

        Func<IQueryable<Threads>, IOrderedQueryable<Threads>> orderBy = normalizedSortBy.ToLowerInvariant() switch
        {
            "createdat" => q => isAsc ? q.OrderBy(t => t.CreatedAt) : q.OrderByDescending(t => t.CreatedAt),
            "updatedat" => q => isAsc ? q.OrderBy(t => t.UpdatedAt) : q.OrderByDescending(t => t.UpdatedAt),
            "title" => q => isAsc ? q.OrderBy(t => t.Title) : q.OrderByDescending(t => t.Title),
            "viewcount" => q => isAsc ? q.OrderBy(t => t.ViewCount) : q.OrderByDescending(t => t.ViewCount),
            _ => q => q.OrderByDescending(t => t.CreatedAt)
        };

        var (threads, totalCount) = await _unitOfWork.Threads.FindPagedAsync(
            predicate: predicate,
            include: query => query
                .Include(t => t.User)
                .Include(t => t.Category),
            orderBy: orderBy,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        return new PagedResultDto<ThreadDto>
        {
            Items = threads.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ThreadDto?> GetThreadByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var thread = await _unitOfWork.Threads.FirstOrDefaultWithIncludesAsync(
            predicate: t => t.Id == id,
            include: query => query
                .Include(t => t.User)
                .Include(t => t.Category),
            cancellationToken: cancellationToken);

        if (thread == null)
        {
            return null;
        }

        // ViewCount artırma işlemi kaldırıldı - artık ayrı endpoint kullanılacak (POST /api/Thread/{id}/view)

        return MapToDto(thread);
    }

    public async Task<ThreadDto> CreateThreadAsync(CreateThreadDto createThreadDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        // MODERASYON: Kullanıcı ban'lı mı kontrol et
        var (isBanned, activeBan) = await _moderationService.IsUserBannedAsync(currentUserId.Value);
        if (isBanned && activeBan != null)
        {
            throw new UnauthorizedAccessException(
                $"Yasaklandığınız için hiçbir işlem yapamazsınız. Bitiş: {activeBan.ExpiresAt:dd.MM.yyyy HH:mm}. Sebep: {activeBan.Reason}");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(createThreadDto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Kategori ID: {createThreadDto.CategoryId} bulunamadı.");
        }
        
        // ÖNEMLI: Kulüp thread'i ise kategori de kulübe ait olmalı
        if (createThreadDto.ClubId.HasValue)
        {
            if (category.ClubId != createThreadDto.ClubId.Value)
            {
                throw new InvalidOperationException("Seçilen kategori bu kulübe ait değil.");
            }
            
            // Kulüp var mı kontrol et
            var club = await _unitOfWork.Clubs.GetByIdAsync(createThreadDto.ClubId.Value, cancellationToken);
            if (club == null)
            {
                throw new KeyNotFoundException($"Kulüp ID: {createThreadDto.ClubId.Value} bulunamadı.");
            }
            
            // Kullanıcı kulüp üyesi mi kontrol et
            var membership = await _unitOfWork.ClubMemberships.FirstOrDefaultAsync(
                m => m.ClubId == createThreadDto.ClubId.Value 
                    && m.UserId == currentUserId.Value 
                    && m.Status == Domain.Enums.MembershipStatus.Approved,
                cancellationToken);
            
            if (membership == null)
            {
                throw new UnauthorizedAccessException("Bu kulüpte konu açmak için üye olmalısınız.");
            }
        }
        else
        {
            // Normal forum thread'i ise kategori de normal olmalı (ClubId == null)
            if (category.ClubId != null)
            {
                throw new InvalidOperationException("Seçilen kategori bir kulübe ait, normal forum kategorisi seçmelisiniz.");
            }
        }

        // 🔇 MODERASYON: Kullanıcı mute'lu mu kontrol et
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
            ClubId = createThreadDto.ClubId, // ÖNEMLI: ClubId'yi set et
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

        // MODERASYON: Kullanıcı ban'lı mı kontrol et
        var (isBanned, activeBan) = await _moderationService.IsUserBannedAsync(currentUserId.Value);
        if (isBanned && activeBan != null)
        {
            throw new UnauthorizedAccessException(
                $"Yasaklandığınız için hiçbir işlem yapamazsınız. Bitiş: {activeBan.ExpiresAt:dd.MM.yyyy HH:mm}. Sebep: {activeBan.Reason}");
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

        // MODERASYON: Kullanıcı ban'lı mı kontrol et
        var (isBanned, activeBan) = await _moderationService.IsUserBannedAsync(currentUserId.Value);
        if (isBanned && activeBan != null)
        {
            throw new UnauthorizedAccessException(
                $"Yasaklandığınız için hiçbir işlem yapamazsınız. Bitiş: {activeBan.ExpiresAt:dd.MM.yyyy HH:mm}. Sebep: {activeBan.Reason}");
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

        // Admin tarafından silinmişse audit log kaydet
        if (isAdmin && thread.UserId != currentUserId.Value)
        {
            var adminUser = await _unitOfWork.Users.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (adminUser != null)
            {
                await _auditLogService.CreateLogAsync(new CreateAuditLogDto
                {
                    UserId = currentUserId.Value,
                    Username = adminUser.Username,
                    Action = "DeleteThread",
                    EntityType = "Thread",
                    EntityId = id,
                    OldValue = $"Title: {thread.Title}",
                    NewValue = "Deleted by Admin",
                    Success = true
                }, cancellationToken);
            }
        }

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
            PostCount = thread.PostCount,
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

    public async Task<bool> IncrementViewCountAsync(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        
        // Login olmuş kullanıcılar için cache ile duplicate view önleme (30 dakika TTL)
        if (currentUserId.HasValue)
        {
            var viewKey = $"thread_view_{id}_user_{currentUserId.Value}";
            
            // Cache'de varsa (30 dk içinde görüntülemiş) artırma
            if (_cache.TryGetValue(viewKey, out _))
            {
                return false; // Zaten görüntülemiş
            }
            
            // Cache'e ekle
            _cache.Set(viewKey, true, TimeSpan.FromMinutes(30));
        }
        // Anonymous kullanıcılar her seferinde sayacak (istenirse IP tracking eklenebilir)
        
        var thread = await _unitOfWork.Threads.GetByIdAsync(id, cancellationToken);
        if (thread == null)
        {
            return false;
        }
        
        thread.ViewCount++;
        _unitOfWork.Threads.Update(thread);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}
