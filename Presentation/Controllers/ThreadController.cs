using Application.DTOs.Thread;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class ThreadController : AppController
{
    private readonly IThreadService _threadService;
    private readonly ILogger<ThreadController> _logger;

    public ThreadController(IThreadService threadService, ILogger<ThreadController> logger)
    {
        _threadService = threadService;
        _logger = logger;
    }

    [HttpGet("getAll")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllThreads(CancellationToken cancellationToken)
    {
        var threads = await _threadService.GetAllThreadsAsync(cancellationToken);
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
        try
        {
            var thread = await _threadService.UpdateThreadAsync(updateThreadDto, cancellationToken);
            return Ok(thread);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("delete/{id}")]
    [Authorize(Roles = "Admin,Moderator")]
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
