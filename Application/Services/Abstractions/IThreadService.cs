using Application.DTOs.Thread;
using Application.DTOs.Common;

namespace Application.Services.Abstractions;

public interface IThreadService
{
    Task<PagedResultDto<ThreadDto>> GetAllThreadsAsync(
        int page = 1,
        int pageSize = 20,
        string? q = null,
        int? categoryId = null,
        bool? isSolved = null,
        int? userId = null,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);
    Task<ThreadDto?> GetThreadByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ThreadDto> CreateThreadAsync(CreateThreadDto createThreadDto, CancellationToken cancellationToken = default);
    Task<ThreadDto> UpdateThreadAsync(UpdateThreadDto updateThreadDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteThreadAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IncrementViewCountAsync(int id, CancellationToken cancellationToken = default);
    
    // Kulüp Thread'leri
    Task<PagedResultDto<ThreadDto>> GetClubThreadsAsync(
        int clubId,
        int page = 1,
        int pageSize = 20,
        string? q = null,
        int? categoryId = null,
        bool? isSolved = null,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);
}
