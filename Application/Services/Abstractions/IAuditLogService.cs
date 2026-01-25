using Application.DTOs.AuditLog;
using Application.DTOs.Common;

namespace Application.Services.Abstractions;

/// <summary>
/// Audit log servisi - Admin işlemlerini kaydetme ve görüntüleme
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Yeni audit log kaydı oluşturur
    /// </summary>
    Task<AuditLogDto> CreateLogAsync(CreateAuditLogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm audit log kayıtlarını getirir (filtrelenmiş, sayfalı)
    /// </summary>
    Task<PagedResultDto<AuditLogListDto>> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir audit log kaydının detayını getirir
    /// </summary>
    Task<AuditLogDto?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir kullanıcının işlemlerini getirir
    /// </summary>
    Task<PagedResultDto<AuditLogListDto>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir entity'nin geçmişini getirir
    /// </summary>
    Task<PagedResultDto<AuditLogListDto>> GetEntityHistoryAsync(string entityType, int entityId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli gün sayısından eski kayıtları siler (90 gün temizliği için)
    /// </summary>
    Task<int> DeleteOlderThanAsync(int days, CancellationToken cancellationToken = default);
}
