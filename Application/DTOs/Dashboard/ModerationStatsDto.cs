namespace Application.DTOs.Dashboard;

/// <summary>
/// Moderasyon istatistikleri
/// </summary>
public record ModerationStatsDto
{
    /// <summary>
    /// Aktif ban sayısı
    /// </summary>
    public int ActiveBans { get; init; }

    /// <summary>
    /// Aktif mute sayısı
    /// </summary>
    public int ActiveMutes { get; init; }

    /// <summary>
    /// Toplam ban sayısı (tüm zamanlar)
    /// </summary>
    public int TotalBans { get; init; }

    /// <summary>
    /// Toplam mute sayısı (tüm zamanlar)
    /// </summary>
    public int TotalMutes { get; init; }

    /// <summary>
    /// Bugün verilen ban sayısı
    /// </summary>
    public int BansToday { get; init; }

    /// <summary>
    /// Bugün verilen mute sayısı
    /// </summary>
    public int MutesToday { get; init; }
}
