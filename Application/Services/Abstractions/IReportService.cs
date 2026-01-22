using Application.DTOs.Common;
using Application.DTOs.Report;
using Domain.Enums;

namespace Application.Services.Abstractions;

/// <summary>
/// Raporlama işlemleri için servis interface'i
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Yeni rapor oluşturur (Kullanıcı tarafından)
    /// </summary>
    /// <param name="dto">Rapor bilgileri</param>
    /// <param name="reporterUserId">Raporu oluşturan kullanıcının ID'si</param>
    /// <returns>Oluşturulan raporun detayları</returns>
    Task<ReportsDto> CreateReportAsync(CreateReportDto dto, int reporterUserId);

    /// <summary>
    /// Kullanıcının kendi oluşturduğu raporları getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Kullanıcının raporları (sayfalı)</returns>
    Task<PagedResultDto<ReportListDto>> GetMyReportsAsync(int userId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Tüm raporları getirir (Admin için)
    /// </summary>
    /// <param name="status">Durum filtresi (opsiyonel)</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Tüm raporlar (sayfalı)</returns>
    Task<PagedResultDto<ReportListDto>> GetAllReportsAsync(ReportStatus? status = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Belirli bir raporun detaylarını getirir
    /// </summary>
    /// <param name="reportId">Rapor ID'si</param>
    /// <param name="requestingUserId">İsteği yapan kullanıcının ID'si (yetki kontrolü için)</param>
    /// <returns>Rapor detayları</returns>
    Task<ReportsDto> GetReportByIdAsync(int reportId, int requestingUserId);

    /// <summary>
    /// Raporun durumunu günceller (Admin tarafından)
    /// </summary>
    /// <param name="reportId">Rapor ID'si</param>
    /// <param name="dto">Güncellenecek durum bilgileri</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Güncellenmiş rapor detayları</returns>
    Task<ReportsDto> UpdateReportStatusAsync(int reportId, UpdateReportStatusDto dto, int adminUserId);

    /// <summary>
    /// Raporu siler (Soft delete)
    /// </summary>
    /// <param name="reportId">Rapor ID'si</param>
    /// <param name="userId">İşlemi yapan kullanıcının ID'si</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> DeleteReportAsync(int reportId, int userId);
}
