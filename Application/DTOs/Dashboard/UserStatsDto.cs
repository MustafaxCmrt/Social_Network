namespace Application.DTOs.Dashboard;

/// <summary>
/// Kullanıcı istatistikleri
/// </summary>
public record UserStatsDto
{
    /// <summary>
    /// Toplam kullanıcı sayısı
    /// </summary>
    public int TotalUsers { get; init; }

    /// <summary>
    /// Aktif kullanıcılar (son 24 saatte giriş yapan)
    /// </summary>
    public int ActiveUsers { get; init; }

    /// <summary>
    /// Bugün kayıt olan kullanıcılar
    /// </summary>
    public int NewUsersToday { get; init; }

    /// <summary>
    /// Bu hafta kayıt olan kullanıcılar
    /// </summary>
    public int NewUsersThisWeek { get; init; }

    /// <summary>
    /// Bu ay kayıt olan kullanıcılar
    /// </summary>
    public int NewUsersThisMonth { get; init; }

    /// <summary>
    /// Admin sayısı
    /// </summary>
    public int TotalAdmins { get; init; }

    /// <summary>
    /// Yasaklı kullanıcı sayısı (aktif ban)
    /// </summary>
    public int BannedUsers { get; init; }
}
