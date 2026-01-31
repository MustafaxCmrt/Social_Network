namespace Domain.Enums;

/// <summary>
/// Bildirim tipleri
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Thread'e yeni yorum geldi
    /// </summary>
    ThreadReply = 1,
    
    /// <summary>
    /// Yoruma cevap geldi
    /// </summary>
    PostReply = 2,
    
    /// <summary>
    /// Yorum çözüm olarak işaretlendi
    /// </summary>
    SolutionMarked = 3,
    
    /// <summary>
    /// Thread çözüldü olarak işaretlendi
    /// </summary>
    ThreadSolved = 4,
    
    /// <summary>
    /// Yorum beğenildi (upvote)
    /// </summary>
    PostUpvoted = 5,
    
    // ==================== KULÜP BİLDİRİMLERİ ====================
    
    /// <summary>
    /// Yeni kulüp açma başvurusu geldi (Moderatörlere)
    /// </summary>
    ClubRequestReceived = 10,
    
    /// <summary>
    /// Kulüp başvurusu onaylandı (Başvurana)
    /// </summary>
    ClubRequestApproved = 11,
    
    /// <summary>
    /// Kulüp başvurusu reddedildi (Başvurana)
    /// </summary>
    ClubRequestRejected = 12,
    
    /// <summary>
    /// Kulübe üyelik başvurusu geldi (Kulüp yöneticilerine)
    /// </summary>
    ClubMembershipRequest = 13,
    
    /// <summary>
    /// Kulüp üyelik başvurusu onaylandı (Başvurana)
    /// </summary>
    ClubMembershipApproved = 14,
    
    /// <summary>
    /// Kulüp üyelik başvurusu reddedildi (Başvurana)
    /// </summary>
    ClubMembershipRejected = 15,
    
    /// <summary>
    /// Kulüpten çıkarıldın (Üyeye)
    /// </summary>
    ClubMemberKicked = 16,
    
    /// <summary>
    /// Kulüpte yeni thread açıldı (Üyelere)
    /// </summary>
    ClubNewThread = 17,
    
    /// <summary>
    /// Kulüp rolün değişti (Üyeye)
    /// </summary>
    ClubRoleChanged = 18,
    
    /// <summary>
    /// Kulüp başkanlığı devredildi (Yeni başkana)
    /// </summary>
    ClubPresidencyTransferred = 19
}
