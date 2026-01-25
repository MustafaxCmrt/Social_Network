namespace Application.DTOs.Dashboard;

/// <summary>
/// Günlük aktivite verisi
/// </summary>
public record DailyActivityDto
{
    /// <summary>
    /// Tarih
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Yeni kullanıcı sayısı
    /// </summary>
    public int NewUsers { get; init; }

    /// <summary>
    /// Oluşturulan thread sayısı
    /// </summary>
    public int NewThreads { get; init; }

    /// <summary>
    /// Oluşturulan post sayısı
    /// </summary>
    public int NewPosts { get; init; }

    /// <summary>
    /// Gelen rapor sayısı
    /// </summary>
    public int NewReports { get; init; }
}
