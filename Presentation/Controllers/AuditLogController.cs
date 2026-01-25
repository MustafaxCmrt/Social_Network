using Application.DTOs.AuditLog;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Audit log (denetim kaydı) işlemleri için controller - Sadece Admin
/// </summary>
[Authorize(Roles = "Admin")]
public class AuditLogController : AppController
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm audit log kayıtlarını getirir (filtrelenmiş, sayfalı)
    /// </summary>
    /// <param name="userId">Kullanıcı ID'ye göre filtre (opsiyonel)</param>
    /// <param name="action">İşlem tipine göre filtre (opsiyonel)</param>
    /// <param name="entityType">Entity tipine göre filtre (opsiyonel)</param>
    /// <param name="entityId">Entity ID'ye göre filtre (opsiyonel)</param>
    /// <param name="success">Başarılı/başarısız filtresi (opsiyonel)</param>
    /// <param name="startDate">Başlangıç tarihi (opsiyonel)</param>
    /// <param name="endDate">Bitiş tarihi (opsiyonel)</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt</param>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int? entityId,
        [FromQuery] bool? success,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AuditLogFilterDto
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Success = success,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var logs = await _auditLogService.GetLogsAsync(filter, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, "An error occurred while retrieving audit logs.");
        }
    }

    /// <summary>
    /// Belirli bir audit log kaydının detayını getirir
    /// </summary>
    /// <param name="id">Audit log ID</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var log = await _auditLogService.GetLogByIdAsync(id, cancellationToken);
            
            if (log == null)
                return NotFound($"Audit log with ID {id} not found.");

            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the audit log.");
        }
    }

    /// <summary>
    /// Belirli bir kullanıcının işlemlerini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt</param>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserLogs(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _auditLogService.GetUserLogsAsync(userId, page, pageSize, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user audit logs.");
        }
    }

    /// <summary>
    /// Belirli bir entity'nin geçmişini getirir
    /// </summary>
    /// <param name="entityType">Entity tipi (User, Post, Thread vb.)</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt</param>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(string entityType, int entityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _auditLogService.GetEntityHistoryAsync(entityType, entityId, page, pageSize, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity history for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving entity history.");
        }
    }

    /// <summary>
    /// 90 günden eski audit log kayıtlarını siler (KVKK/GDPR uyumu için)
    /// </summary>
    /// <param name="days">Kaç günden eski kayıtlar silinecek (varsayılan: 90)</param>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldLogs([FromQuery] int days = 90, CancellationToken cancellationToken = default)
    {
        if (days < 1)
            return BadRequest("Days must be greater than 0.");

        try
        {
            var deletedCount = await _auditLogService.DeleteOlderThanAsync(days, cancellationToken);
            _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days", deletedCount, days);
            
            return Ok(new { 
                message = $"Successfully deleted {deletedCount} audit log(s) older than {days} days.",
                deletedCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old audit logs");
            return StatusCode(500, "An error occurred while cleaning up old audit logs.");
        }
    }
}
