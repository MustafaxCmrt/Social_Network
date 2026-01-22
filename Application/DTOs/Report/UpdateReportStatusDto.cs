using Domain.Enums;

namespace Application.DTOs.Report;

/// <summary>
/// Admin tarafından rapor durumunu güncellemek için kullanılan DTO
/// </summary>
public record UpdateReportStatusDto
{
    /// <summary>
    /// Yeni rapor durumu (Reviewed, Resolved, Rejected)
    /// </summary>
    public ReportStatus Status { get; init; }
    
    /// <summary>
    /// Admin notu (opsiyonel, max 500 karakter)
    /// </summary>
    public string? AdminNote { get; init; }
}
