using Application.DTOs.Thread;

namespace Application.Services.Abstractions;

public interface IThreadService
{
    Task<IEnumerable<ThreadDto>> GetAllThreadsAsync(CancellationToken cancellationToken = default);
    Task<ThreadDto?> GetThreadByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ThreadDto> CreateThreadAsync(CreateThreadDto createThreadDto, CancellationToken cancellationToken = default);
    Task<ThreadDto> UpdateThreadAsync(UpdateThreadDto updateThreadDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteThreadAsync(int id, CancellationToken cancellationToken = default);
}
