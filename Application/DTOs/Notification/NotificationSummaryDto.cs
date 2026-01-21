namespace Application.DTOs.Notification;

/// <summary>
/// Bildirim özeti - Badge sayısı için kullanılır
/// </summary>
public class NotificationSummaryDto
{
    /// <summary>
    /// Okunmamış bildirim sayısı
    /// </summary>
    public int UnreadCount { get; set; }
    
    /// <summary>
    /// Toplam bildirim sayısı
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// En son bildirim zamanı (yoksa null)
    /// </summary>
    public DateTime? LastNotificationDate { get; set; }
}
