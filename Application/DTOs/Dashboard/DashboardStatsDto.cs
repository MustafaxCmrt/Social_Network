namespace Application.DTOs.Dashboard;

/// <summary>
/// Dashboard için genel istatistik verileri
/// </summary>
public record DashboardStatsDto
{
    /// <summary>
    /// Kullanıcı istatistikleri
    /// </summary>
    public UserStatsDto UserStats { get; init; } = null!;

    /// <summary>
    /// İçerik istatistikleri
    /// </summary>
    public ContentStatsDto ContentStats { get; init; } = null!;

    /// <summary>
    /// Rapor istatistikleri
    /// </summary>
    public ReportStatsDto ReportStats { get; init; } = null!;

    /// <summary>
    /// Moderasyon istatistikleri
    /// </summary>
    public ModerationStatsDto ModerationStats { get; init; } = null!;

    /// <summary>
    /// Son 7 günlük aktivite grafiği
    /// </summary>
    public List<DailyActivityDto> Last7DaysActivity { get; init; } = new();
}
