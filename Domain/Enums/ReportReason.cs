namespace Domain.Enums;

/// <summary>
/// Rapor sebepleri
/// </summary>
public enum ReportReason
{
    /// <summary>
    /// Spam içerik
    /// </summary>
    Spam = 1,
    
    /// <summary>
    /// Taciz/Hakaret
    /// </summary>
    Harassment = 2,
    
    /// <summary>
    /// Uygunsuz içerik
    /// </summary>
    InappropriateContent = 3,
    
    /// <summary>
    /// Yanlış/Yanıltıcı bilgi
    /// </summary>
    Misinformation = 4,
    
    /// <summary>
    /// Diğer sebepler
    /// </summary>
    Other = 5
}
