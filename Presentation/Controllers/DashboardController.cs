using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Admin dashboard istatistikleri için controller
/// </summary>
public class DashboardController : AppController
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Genel dashboard istatistiklerini getirir (Admin)
    /// </summary>
    /// <returns>Dashboard istatistikleri</returns>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, "An error occurred while retrieving dashboard statistics.");
        }
    }

    /// <summary>
    /// En aktif kullanıcıları getirir (Admin)
    /// </summary>
    /// <param name="topCount">Kaç kullanıcı getirileceği (varsayılan: 10, max: 50)</param>
    /// <returns>En aktif kullanıcı listesi</returns>
    [HttpGet("top-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTopUsers([FromQuery] int topCount = 10, CancellationToken cancellationToken = default)
    {
        if (topCount < 1 || topCount > 50)
        {
            return BadRequest("Top count must be between 1 and 50.");
        }

        try
        {
            var topUsers = await _dashboardService.GetTopUsersAsync(topCount, cancellationToken);
            return Ok(topUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top users");
            return StatusCode(500, "An error occurred while retrieving top users.");
        }
    }

    /// <summary>
    /// En çok raporlanan içerikleri getirir (Admin)
    /// </summary>
    /// <param name="topCount">Kaç içerik getirileceği (varsayılan: 10, max: 50)</param>
    /// <returns>En çok raporlanan içerik listesi</returns>
    [HttpGet("top-reported")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTopReportedContent([FromQuery] int topCount = 10, CancellationToken cancellationToken = default)
    {
        if (topCount < 1 || topCount > 50)
        {
            return BadRequest("Top count must be between 1 and 50.");
        }

        try
        {
            var topReported = await _dashboardService.GetTopReportedContentAsync(topCount, cancellationToken);
            return Ok(topReported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top reported content");
            return StatusCode(500, "An error occurred while retrieving top reported content.");
        }
    }
}
