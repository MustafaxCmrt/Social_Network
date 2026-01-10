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
    
    // Upvote işlemleri
    Task<UpvoteResponseDto> UpvotePostAsync(int postId, int userId, CancellationToken cancellationToken = default);
    Task<UpvoteResponseDto> RemoveUpvoteAsync(int postId, int userId, CancellationToken cancellationToken = default);
    Task<VoteStatusDto> GetVoteStatusAsync(int postId, int userId, CancellationToken cancellationToken = default);
    
    // Nested comment işlemleri
    Task<PagedResultDto<PostDto>> GetPostRepliesAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default);
}
