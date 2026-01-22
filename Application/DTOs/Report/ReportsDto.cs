using Domain.Enums;

namespace Application.DTOs.Report;

/// <summary>
/// Rapor detay bilgilerini içeren DTO
/// </summary>
public record ReportsDto
{
    public int Id { get; init; }
    
    // Raporlayan kişi bilgileri
    public int ReporterId { get; init; }
    public string ReporterUsername { get; init; } = string.Empty;
    public string ReporterEmail { get; init; } = string.Empty;
    
    // Raporlanan içerik/kullanıcı bilgileri
    public int? ReportedUserId { get; init; }
    public string? ReportedUsername { get; init; }
    
    public int? ReportedPostId { get; init; }
    public string? PostTitle { get; init; } // Post başlığı veya içeriğin ilk 100 karakteri
    
    public int? ReportedThreadId { get; init; }
    public string? ThreadTitle { get; init; }
    
    // Rapor bilgileri
    public ReportReason Reason { get; init; }
    public string? Description { get; init; }
    public ReportStatus Status { get; init; }
    
    // İnceleme bilgileri
    public int? ReviewedByUserId { get; init; }
    public string? ReviewedByUsername { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? AdminNote { get; init; }
    
    // Zaman bilgileri
    public DateTime CreatedAt { get; init; }
}
