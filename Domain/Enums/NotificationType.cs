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
    ThreadSolved = 4
}
