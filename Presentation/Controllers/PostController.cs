using Application.DTOs.Post;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class PostController : AppController
{
    private readonly IPostService _postService;
    private readonly ILogger<PostController> _logger;

    public PostController(IPostService postService, ILogger<PostController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    [HttpGet("getAll/{threadId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPostsByThreadId(int threadId, CancellationToken cancellationToken)
    {
        var posts = await _postService.GetAllPostsByThreadIdAsync(threadId, cancellationToken);
        return Ok(posts);
    }

    [HttpGet("get/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(int id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetPostByIdAsync(id, cancellationToken);

        if (post == null)
        {
            return NotFound(new { message = $"ID: {id} olan yorum bulunamadı." });
        }

        return Ok(post);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto createPostDto, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _postService.CreatePostAsync(createPostDto, cancellationToken);
            return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post create unauthorized");
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> UpdatePost([FromBody] UpdatePostDto updatePostDto, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _postService.UpdatePostAsync(updatePostDto, cancellationToken);
            return Ok(post);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post update forbidden");
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _postService.DeletePostAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"ID: {id} olan yorum bulunamadı." });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post delete forbidden");
            return Forbid();
        }
    }

    [HttpPost("markSolution")]
    [Authorize]
    public async Task<IActionResult> MarkSolution([FromBody] MarkSolutionDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _postService.MarkSolutionAsync(request, cancellationToken);
            return Ok(new { success = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "MarkSolution forbidden");
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
