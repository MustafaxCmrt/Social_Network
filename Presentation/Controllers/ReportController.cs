using Application.DTOs.Report;
using Application.Services.Abstractions;
using Application.Validations.Report;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Raporlama işlemleri için controller
/// </summary>
public class ReportController : AppController
{
    private readonly IReportService _reportService;
    private readonly IValidator<CreateReportDto> _createReportValidator;
    private readonly IValidator<UpdateReportStatusDto> _updateReportStatusValidator;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportService reportService,
        IValidator<CreateReportDto> createReportValidator,
        IValidator<UpdateReportStatusDto> updateReportStatusValidator,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _createReportValidator = createReportValidator;
        _updateReportStatusValidator = updateReportStatusValidator;
        _logger = logger;
    }

    /// <summary>
    /// Yeni rapor oluşturur (kullanıcı veya post veya thread raporu)
    /// </summary>
    /// <param name="dto">Rapor bilgileri</param>
    /// <returns>Oluşturulan rapor</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
    {
        // Validation
        var validationResult = await _createReportValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var userId))
        {
            return Unauthorized("Kullanıcı doğrulaması başarısız.");
        }

        try
        {
            var report = await _reportService.CreateReportAsync(dto, userId);
            _logger.LogInformation("Report created successfully by user {UserId}", userId);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid report creation attempt by user {UserId}", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report for user {UserId}", userId);
            return StatusCode(500, "Rapor oluşturulurken bir hata oluştu.");
        }
    }

    /// <summary>
    /// Kullanıcının kendi oluşturduğu raporları getirir
    /// </summary>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Kullanıcının raporları (sayfalı)</returns>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var userId))
        {
            return Unauthorized("Kullanıcı doğrulaması başarısız.");
        }

        try
        {
            var reports = await _reportService.GetMyReportsAsync(userId, page, pageSize);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for user {UserId}", userId);
            return StatusCode(500, "Raporlar getirilirken bir hata oluştu.");
        }
    }

    /// <summary>
    /// Tüm raporları getirir (Admin için)
    /// </summary>
    /// <param name="status">Durum filtresi (opsiyonel)</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Tüm raporlar (sayfalı)</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReports(
        [FromQuery] ReportStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var reports = await _reportService.GetAllReportsAsync(status, page, pageSize);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all reports");
            return StatusCode(500, "Raporlar getirilirken bir hata oluştu.");
        }
    }

    /// <summary>
    /// Belirli bir raporun detaylarını getirir
    /// </summary>
    /// <param name="id">Rapor ID'si</param>
    /// <returns>Rapor detayları</returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetReportById(int id)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var userId))
        {
            return Unauthorized("Kullanıcı doğrulaması başarısız.");
        }

        try
        {
            var report = await _reportService.GetReportByIdAsync(id, userId);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report {ReportId} not found", id);
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to report {ReportId} by user {UserId}", id, userId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", id);
            return StatusCode(500, "Rapor getirilirken bir hata oluştu.");
        }
    }

    /// <summary>
    /// Raporun durumunu günceller (Admin tarafından)
    /// </summary>
    /// <param name="id">Rapor ID'si</param>
    /// <param name="dto">Güncellenecek durum bilgileri</param>
    /// <returns>Güncellenmiş rapor</returns>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateReportStatusDto dto)
    {
        // Validation
        var validationResult = await _updateReportStatusValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var userId))
        {
            return Unauthorized("Kullanıcı doğrulaması başarısız.");
        }

        try
        {
            var report = await _reportService.UpdateReportStatusAsync(id, dto, userId);
            _logger.LogInformation("Report {ReportId} status updated to {Status} by admin {AdminId}", 
                id, dto.Status, userId);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report {ReportId} not found for status update", id);
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized status update attempt for report {ReportId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for report {ReportId}", id);
            return StatusCode(500, "Rapor durumu güncellenirken bir hata oluştu.");
        }
    }

    /// <summary>
    /// Raporu siler (Soft delete - Admin için)
    /// </summary>
    /// <param name="id">Rapor ID'si</param>
    /// <returns>Başarılı ise true</returns>
    [HttpDelete("delete/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReport(int id)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var userId))
        {
            return Unauthorized("Kullanıcı doğrulaması başarısız.");
        }

        try
        {
            var result = await _reportService.DeleteReportAsync(id, userId);
            if (result)
            {
                _logger.LogInformation("Report {ReportId} deleted by admin {AdminId}", id, userId);
                return Ok(new { message = "Rapor başarıyla silindi." });
            }

            return BadRequest("Rapor silinirken bir hata oluştu.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report {ReportId} not found for deletion", id);
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized deletion attempt for report {ReportId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {ReportId}", id);
            return StatusCode(500, "Rapor silinirken bir hata oluştu.");
        }
    }
}
