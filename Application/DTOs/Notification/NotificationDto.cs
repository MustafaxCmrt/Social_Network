using Domain.Enums;

namespace Application.DTOs.Notification;

/// <summary>
/// Kullanıcıya gönderilecek bildirim DTO'su
/// Frontend'de bildirim listesinde gösterilir
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // İşlemi yapan kullanıcı bilgileri (nullable - sistem bildirimi olabilir)
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public string? ActorFirstName { get; set; }
    public string? ActorLastName { get; set; }
    
    // İlgili içerik bilgileri (link için)
    public int? ThreadId { get; set; }
    public string? ThreadTitle { get; set; }
    public int? PostId { get; set; }
}
