using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Kulüp üyelikleri
/// Kullanıcıların hangi kulüplere üye olduğu ve rolleri
/// </summary>
public class ClubMemberships : BaseEntity
{
    /// <summary>
    /// Kulüp ID (FK)
    /// </summary>
    public int ClubId { get; set; }
    
    /// <summary>
    /// Kullanıcı ID (FK)
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Kulüp içi rol (Member, Officer, VicePresident, President)
    /// </summary>
    public ClubRole Role { get; set; } = ClubRole.Member;
    
    /// <summary>
    /// Üyelik durumu (Pending, Approved, Rejected, Left, Kicked)
    /// </summary>
    public MembershipStatus Status { get; set; } = MembershipStatus.Pending;
    
    /// <summary>
    /// Üyeliğin onaylandığı tarih
    /// </summary>
    public DateTime? JoinedAt { get; set; }
    
    /// <summary>
    /// Üyelik başvuru notu (opsiyonel)
    /// </summary>
    public string? JoinNote { get; set; }
    
    // ==================== NAVIGATION PROPERTIES ====================
    
    /// <summary>
    /// Üyelik yapılan kulüp
    /// </summary>
    public Clubs Club { get; set; } = null!;
    
    /// <summary>
    /// Üye olan kullanıcı
    /// </summary>
    public Users User { get; set; } = null!;
}
