using Application.DTOs.Post;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class PostController : AppController
{
    private readonly IPostService _postService;
    private readonly ILogger<PostController> _logger;
    private readonly IValidator<CreatePostDto> _createValidator;
    private readonly IValidator<UpdatePostDto> _updateValidator;
    private readonly IValidator<MarkSolutionDto> _markSolutionValidator;

    public PostController(
        IPostService postService,
        ILogger<PostController> logger,
        IValidator<CreatePostDto> createValidator,
        IValidator<UpdatePostDto> updateValidator,
        IValidator<MarkSolutionDto> markSolutionValidator)
    {
        _postService = postService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _markSolutionValidator = markSolutionValidator;
    }

    [HttpGet("getAll/{threadId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPostsByThreadId(
        int threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var posts = await _postService.GetAllPostsByThreadIdAsync(threadId, page, pageSize, cancellationToken);
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
        var validationResult = await _createValidator.ValidateAsync(createPostDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

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
        var validationResult = await _updateValidator.ValidateAsync(updatePostDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

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
        var validationResult = await _markSolutionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

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
