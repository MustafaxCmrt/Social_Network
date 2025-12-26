using Application.DTOs.Thread;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

public class ThreadService : IThreadService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ThreadService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ThreadDto>> GetAllThreadsAsync(CancellationToken cancellationToken = default)
    {
        var threads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: query => query.Include(t => t.User).Include(t => t.Category),
            cancellationToken);

        return threads.Select(MapToDto);
    }

    public async Task<ThreadDto?> GetThreadByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var threads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: query => query.Include(t => t.User).Include(t => t.Category),
            cancellationToken);

        var thread = threads.FirstOrDefault(t => t.Id == id);
        return thread == null ? null : MapToDto(thread);
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
        var thread = await _unitOfWork.Threads.GetByIdAsync(updateThreadDto.Id, cancellationToken);
        if (thread == null)
        {
            throw new KeyNotFoundException($"ID: {updateThreadDto.Id} olan konu bulunamadı.");
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
        var thread = await _unitOfWork.Threads.GetByIdAsync(id, cancellationToken);
        if (thread == null)
        {
            return false;
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
            UpdatedAt = thread.UpdatedAt
        };
    }
}
