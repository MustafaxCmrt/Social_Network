namespace Domain.Enums;

/// <summary>
/// Kulüp açma başvurusu durumları
/// </summary>
public enum ClubRequestStatus
{
    /// <summary>
    /// Beklemede - Moderatör incelemesi bekleniyor
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Onaylandı - Kulüp oluşturuldu
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Reddedildi - Başvuru reddedildi
    /// </summary>
    Rejected = 2
}
