using Application.DTOs.Dashboard;

namespace Application.Services.Abstractions;

/// <summary>
/// Dashboard istatistikleri için servis interface'i
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Genel dashboard istatistiklerini getirir
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Dashboard istatistikleri</returns>
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// En aktif kullanıcıları getirir
    /// </summary>
    /// <param name="topCount">Kaç kullanıcı getirileceği (varsayılan: 10)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>En aktif kullanıcı listesi</returns>
    Task<List<TopUserDto>> GetTopUsersAsync(int topCount = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// En çok raporlanan içerikleri getirir
    /// </summary>
    /// <param name="topCount">Kaç içerik getirileceği (varsayılan: 10)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>En çok raporlanan içerik listesi</returns>
    Task<List<TopReportedContentDto>> GetTopReportedContentAsync(int topCount = 10, CancellationToken cancellationToken = default);
}
