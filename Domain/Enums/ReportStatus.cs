namespace Domain.Enums;

/// <summary>
/// Rapor durumu
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Beklemede - Admin henüz incelemedi
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// İncelendi - Admin baktı
    /// </summary>
    Reviewed = 2,
    
    /// <summary>
    /// Çözüldü - Aksiyon alındı (içerik silindi, kullanıcı banlandı vb.)
    /// </summary>
    Resolved = 3,
    
    /// <summary>
    /// Reddedildi - Haksız rapor
    /// </summary>
    Rejected = 4
}
