using Domain.Enums;

namespace Application.DTOs.Report;

/// <summary>
/// Yeni rapor oluşturmak için kullanılan DTO
/// </summary>
public record CreateReportDto
{
    /// <summary>
    /// Raporlanan kullanıcı ID (opsiyonel)
    /// </summary>
    public int? ReportedUserId { get; init; }
    
    /// <summary>
    /// Raporlanan post ID (opsiyonel)
    /// </summary>
    public int? ReportedPostId { get; init; }
    
    /// <summary>
    /// Raporlanan thread ID (opsiyonel)
    /// </summary>
    public int? ReportedThreadId { get; init; }
    
    /// <summary>
    /// Raporlama sebebi (zorunlu)
    /// </summary>
    public ReportReason Reason { get; init; }
    
    /// <summary>
    /// Raporlama açıklaması (opsiyonel, max 1000 karakter)
    /// </summary>
    public string? Description { get; init; }
}
