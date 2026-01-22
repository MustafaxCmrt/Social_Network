using Domain.Enums;

namespace Application.DTOs.Report;

/// <summary>
/// Admin panelinde rapor listesi için kullanılan DTO
/// </summary>
public record ReportListDto
{
    public int Id { get; init; }
    
    // Raporlayan
    public string ReporterUsername { get; init; } = string.Empty;
    
    // Raporlanan içerik tipi ve özet bilgi
    public string ReportedType { get; init; } = string.Empty; // "User", "Post", "Thread"
    public string ReportedInfo { get; init; } = string.Empty; // Username veya başlık
    
    // Rapor bilgileri
    public ReportReason Reason { get; init; }
    public ReportStatus Status { get; init; }
    
    // Zaman
    public DateTime CreatedAt { get; init; }
}
