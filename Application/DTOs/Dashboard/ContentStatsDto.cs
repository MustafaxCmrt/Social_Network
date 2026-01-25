namespace Application.DTOs.Dashboard;

/// <summary>
/// İçerik istatistikleri
/// </summary>
public record ContentStatsDto
{
    /// <summary>
    /// Toplam thread sayısı
    /// </summary>
    public int TotalThreads { get; init; }

    /// <summary>
    /// Toplam post sayısı
    /// </summary>
    public int TotalPosts { get; init; }

    /// <summary>
    /// Bugün oluşturulan thread sayısı
    /// </summary>
    public int ThreadsToday { get; init; }

    /// <summary>
    /// Bugün oluşturulan post sayısı
    /// </summary>
    public int PostsToday { get; init; }

    /// <summary>
    /// Bu hafta oluşturulan thread sayısı
    /// </summary>
    public int ThreadsThisWeek { get; init; }

    /// <summary>
    /// Bu hafta oluşturulan post sayısı
    /// </summary>
    public int PostsThisWeek { get; init; }

    /// <summary>
    /// Kilitli thread sayısı
    /// </summary>
    public int LockedThreads { get; init; }

    /// <summary>
    /// Toplam kategori sayısı
    /// </summary>
    public int TotalCategories { get; init; }
}
