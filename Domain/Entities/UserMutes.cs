using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Kullanıcı susturma (mute) kayıtları
/// </summary>
public class UserMutes : BaseEntity
{
    /// <summary>
    /// Susturulan kullanıcının ID'si (zorunlu)
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Susturmayı uygulayan admin/moderator ID'si (zorunlu)
    /// </summary>
    public int MutedByUserId { get; set; }
    
    /// <summary>
    /// Susturma sebebi (zorunlu)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Susturma tarihi (zorunlu, otomatik)
    /// </summary>
    public DateTime MutedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Susturma bitiş tarihi (mute her zaman geçici olmalı, zorunlu)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Susturma aktif mi? (false = kaldırıldı/süresi doldu)
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    
    /// <summary>
    /// Susturulan kullanıcı
    /// </summary>
    public Users User { get; set; } = null!;
    
    /// <summary>
    /// Susturmayı uygulayan admin/moderator
    /// </summary>
    public Users MutedByUser { get; set; } = null!;
}
