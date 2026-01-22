using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Kullanıcı yasaklama (ban) kayıtları
/// </summary>
public class UserBans : BaseEntity
{
    /// <summary>
    /// Yasaklanan kullanıcının ID'si (zorunlu)
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Yasağı uygulayan admin/moderator ID'si (zorunlu)
    /// </summary>
    public int BannedByUserId { get; set; }
    
    /// <summary>
    /// Yasaklama sebebi (zorunlu)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Yasaklama tarihi (zorunlu, otomatik)
    /// </summary>
    public DateTime BannedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Yasak bitiş tarihi (null = kalıcı ban)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Yasak aktif mi? (false = kaldırıldı/süresi doldu)
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    
    /// <summary>
    /// Yasaklanan kullanıcı
    /// </summary>
    public Users User { get; set; } = null!;
    
    /// <summary>
    /// Yasağı uygulayan admin/moderator
    /// </summary>
    public Users BannedByUser { get; set; } = null!;
}
