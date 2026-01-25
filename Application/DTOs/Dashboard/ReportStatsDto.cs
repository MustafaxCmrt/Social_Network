namespace Application.DTOs.Dashboard;

/// <summary>
/// Rapor istatistikleri
/// </summary>
public record ReportStatsDto
{
    /// <summary>
    /// Toplam rapor sayısı
    /// </summary>
    public int TotalReports { get; init; }

    /// <summary>
    /// Bekleyen rapor sayısı
    /// </summary>
    public int PendingReports { get; init; }

    /// <summary>
    /// İncelenen rapor sayısı
    /// </summary>
    public int ReviewedReports { get; init; }

    /// <summary>
    /// Çözülen rapor sayısı
    /// </summary>
    public int ResolvedReports { get; init; }

    /// <summary>
    /// Reddedilen rapor sayısı
    /// </summary>
    public int RejectedReports { get; init; }

    /// <summary>
    /// Bugün gelen rapor sayısı
    /// </summary>
    public int ReportsToday { get; init; }

    /// <summary>
    /// Bu hafta gelen rapor sayısı
    /// </summary>
    public int ReportsThisWeek { get; init; }
}
