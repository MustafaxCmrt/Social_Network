using Application.DTOs.Post;

namespace Application.Services.Abstractions;

public interface IPostService
{
    Task<IEnumerable<PostDto>> GetAllPostsByThreadIdAsync(int threadId, CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PostDto> CreatePostAsync(CreatePostDto createPostDto, CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostAsync(UpdatePostDto updatePostDto, CancellationToken cancellationToken = default);
    Task<bool> DeletePostAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> MarkSolutionAsync(MarkSolutionDto request, CancellationToken cancellationToken = default);
}
