using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Kulüp açma başvuruları
/// Öğrenciler kulüp açmak için başvuru yapar, moderatörler onaylar/reddeder
/// </summary>
public class ClubRequests : BaseEntity
{
    /// <summary>
    /// İstenen kulüp adı
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Kulüp açıklaması
    /// </summary>
    public string Description { get; set; } = null!;
    
    /// <summary>
    /// Kulübün amacı ve hedefleri
    /// </summary>
    public string Purpose { get; set; } = null!;
    
    /// <summary>
    /// Başvuru durumu
    /// </summary>
    public ClubRequestStatus Status { get; set; } = ClubRequestStatus.Pending;
    
    /// <summary>
    /// Başvuruyu yapan kullanıcı ID (FK)
    /// </summary>
    public int RequestedByUserId { get; set; }
    
    /// <summary>
    /// İnceleyen moderatör/admin ID (FK) - Nullable
    /// </summary>
    public int? ReviewedByUserId { get; set; }
    
    /// <summary>
    /// İnceleme tarihi
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
    
    /// <summary>
    /// Red sebebi (reddedilmişse)
    /// </summary>
    public string? RejectionReason { get; set; }
    
    // ==================== NAVIGATION PROPERTIES ====================
    
    /// <summary>
    /// Başvuruyu yapan kullanıcı
    /// </summary>
    public Users RequestedByUser { get; set; } = null!;
    
    /// <summary>
    /// İnceleyen moderatör/admin
    /// </summary>
    public Users? ReviewedByUser { get; set; }
}
