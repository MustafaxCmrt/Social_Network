using Application.DTOs.Thread;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class ThreadController : AppController
{
    private readonly IThreadService _threadService;
    private readonly ILogger<ThreadController> _logger;
    private readonly IValidator<CreateThreadDto> _createValidator;
    private readonly IValidator<UpdateThreadDto> _updateValidator;

    public ThreadController(
        IThreadService threadService,
        ILogger<ThreadController> logger,
        IValidator<CreateThreadDto> createValidator,
        IValidator<UpdateThreadDto> updateValidator)
    {
        _threadService = threadService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("getAll")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllThreads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isSolved = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var threads = await _threadService.GetAllThreadsAsync(
            page,
            pageSize,
            q,
            categoryId,
            isSolved,
            userId,
            sortBy,
            sortDir,
            cancellationToken);
        return Ok(threads);
    }

    [HttpGet("get/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThreadById(int id, CancellationToken cancellationToken)
    {
        var thread = await _threadService.GetThreadByIdAsync(id, cancellationToken);
        if (thread == null)
        {
            return NotFound(new { message = $"ID: {id} olan konu bulunamadı." });
        }

        return Ok(thread);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateThread([FromBody] CreateThreadDto createThreadDto, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(createThreadDto, cancellationToken);
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
            var thread = await _threadService.CreateThreadAsync(createThreadDto, cancellationToken);
            return CreatedAtAction(nameof(GetThreadById), new { id = thread.Id }, thread);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Thread create unauthorized");
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> UpdateThread([FromBody] UpdateThreadDto updateThreadDto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateThreadDto, cancellationToken);
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
            var thread = await _threadService.UpdateThreadAsync(updateThreadDto, cancellationToken);
            return Ok(thread);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Thread update forbidden");
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteThread(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _threadService.DeleteThreadAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"ID: {id} olan konu bulunamadı." });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Thread delete forbidden");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
