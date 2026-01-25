namespace Application.DTOs.Dashboard;

/// <summary>
/// En çok raporlanan içerik bilgisi
/// </summary>
public record TopReportedContentDto
{
    /// <summary>
    /// İçerik ID (Thread veya Post veya User ID)
    /// </summary>
    public int ContentId { get; init; }

    /// <summary>
    /// İçerik tipi (User, Thread, Post)
    /// </summary>
    public string ContentType { get; init; } = null!;

    /// <summary>
    /// İçerik başlığı/önizlemesi
    /// </summary>
    public string ContentPreview { get; init; } = null!;

    /// <summary>
    /// Rapor sayısı
    /// </summary>
    public int ReportCount { get; init; }

    /// <summary>
    /// Son rapor tarihi
    /// </summary>
    public DateTime LastReportedAt { get; init; }
}
