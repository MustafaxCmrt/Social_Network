using Domain.Enums;

namespace Application.DTOs.Notification;

/// <summary>
/// Yeni bildirim oluşturmak için kullanılır (internal - servisler arası)
/// Controller'da kullanılmaz, sadece servisler içinde
/// </summary>
public class CreateNotificationDto
{
    public int UserId { get; set; } // Kime gidecek?
    public int? ActorUserId { get; set; } // Kim yaptı?
    public NotificationType Type { get; set; } // Ne oldu?
    public string Message { get; set; } = null!; // Mesaj
    public int? ThreadId { get; set; } // Hangi thread?
    public int? PostId { get; set; } // Hangi post?
}
