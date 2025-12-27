using Application.DTOs.Post;
using Application.DTOs.Common;

namespace Application.Services.Abstractions;

public interface IPostService
{
    Task<PagedResultDto<PostDto>> GetAllPostsByThreadIdAsync(
        int threadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PostDto> CreatePostAsync(CreatePostDto createPostDto, CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostAsync(UpdatePostDto updatePostDto, CancellationToken cancellationToken = default);
    Task<bool> DeletePostAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> MarkSolutionAsync(MarkSolutionDto request, CancellationToken cancellationToken = default);
}
