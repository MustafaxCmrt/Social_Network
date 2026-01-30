namespace Domain.Enums;

/// <summary>
/// Kulüp üyelik durumları
/// </summary>
public enum MembershipStatus
{
    /// <summary>
    /// Beklemede - Üyelik onayı bekleniyor
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Onaylandı - Aktif üye
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Reddedildi - Üyelik başvurusu reddedildi
    /// </summary>
    Rejected = 2,
    
    /// <summary>
    /// Ayrıldı - Kendi isteğiyle kulüpten ayrıldı
    /// </summary>
    Left = 3,
    
    /// <summary>
    /// Atıldı - Kulüpten çıkarıldı
    /// </summary>
    Kicked = 4
}
