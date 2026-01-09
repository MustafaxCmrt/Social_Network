using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Kullanıcı bildirimleri tablosu
/// Thread'e yorum, yoruma cevap, çözüm seçilmesi gibi olaylar için
/// </summary>
public class Notifications : BaseEntity
{
    public int UserId { get; set; } // Bildirimi alan kullanıcı - Zorunlu
    public int? ActorUserId { get; set; } // İşlemi yapan kullanıcı - Opsiyonel (sistem bildirimi olabilir)
    public NotificationType Type { get; set; } // Bildirim tipi - Zorunlu
    public string Message { get; set; } = null!; // Bildirim mesajı - Zorunlu
    
    public int? ThreadId { get; set; } // İlgili thread - Opsiyonel
    public int? PostId { get; set; } // İlgili post - Opsiyonel
    
    public bool IsRead { get; set; } = false; // Okundu mu? - Varsayılan: false

    // NAVIGATION PROPERTIES
    /// <summary>
    /// Bildirimi alan kullanıcı
    /// </summary>
    public Users User { get; set; } = null!;
    
    /// <summary>
    /// İşlemi yapan kullanıcı (opsiyonel)
    /// </summary>
    public Users? ActorUser { get; set; }
    
    /// <summary>
    /// İlgili thread (opsiyonel)
    /// </summary>
    public Threads? Thread { get; set; }
    
    /// <summary>
    /// İlgili post (opsiyonel)
    /// </summary>
    public Posts? Post { get; set; }
}
