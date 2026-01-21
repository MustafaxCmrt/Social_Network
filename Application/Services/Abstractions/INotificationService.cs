using Application.DTOs.Common;
using Application.DTOs.Notification;

namespace Application.Services.Abstractions;

/// <summary>
/// Bildirim yönetim servisi
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Kullanıcının tüm bildirimlerini getirir (sayfalama ile)
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <param name="onlyUnread">Sadece okunmamışları getir mi?</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>Sayfalanmış bildirim listesi</returns>
    Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(
        int userId, 
        int page = 1, 
        int pageSize = 20, 
        bool onlyUnread = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının bildirim özetini getirir (badge sayısı için)
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>Bildirim özeti</returns>
    Task<NotificationSummaryDto> GetNotificationSummaryAsync(
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir bildirimi okundu olarak işaretler
    /// </summary>
    /// <param name="notificationId">Bildirim ID</param>
    /// <param name="userId">Kullanıcı ID (güvenlik kontrolü için)</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> MarkAsReadAsync(
        int notificationId, 
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretler
    /// </summary>
    /// <param name="userId">Kullanıcı ID</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>İşleme alınan bildirim sayısı</returns>
    Task<int> MarkAllAsReadAsync(
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir bildirimi siler
    /// </summary>
    /// <param name="notificationId">Bildirim ID</param>
    /// <param name="userId">Kullanıcı ID (güvenlik kontrolü için)</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> DeleteNotificationAsync(
        int notificationId, 
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeni bildirim oluşturur (INTERNAL - diğer servisler kullanır)
    /// PostService, ThreadService vb. bu metodu çağırır
    /// </summary>
    /// <param name="dto">Bildirim bilgileri</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>Oluşturulan bildirim ID</returns>
    Task<int> CreateNotificationAsync(
        CreateNotificationDto dto, 
        CancellationToken cancellationToken = default);
}
