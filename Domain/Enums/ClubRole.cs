namespace Domain.Enums;

/// <summary>
/// Kulüp içi roller (üyelik seviyesi)
/// </summary>
public enum ClubRole
{
    /// <summary>
    /// Normal üye
    /// </summary>
    Member = 0,
    
    /// <summary>
    /// Yönetim kurulu üyesi
    /// </summary>
    Officer = 1,
    
    /// <summary>
    /// Başkan yardımcısı
    /// </summary>
    VicePresident = 2,
    
    /// <summary>
    /// Kulüp başkanı (en yetkili)
    /// </summary>
    President = 3
}
