using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Şifre sıfırlama token'larını tutar
/// Guid email ile gönderilir, veritabanında hash'lenmiş hali tutulur
/// </summary>
public class PasswordResetTokens : BaseEntity
{
    /// <summary>
    /// Token sahibi kullanıcı ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Hash'lenmiş reset token (SHA256)
    /// Email'de düz metin gönderilir, DB'de hash tutulur
    /// </summary>
    public string Guid { get; set; } = string.Empty;
    
    /// <summary>
    /// Token'ın geçerlilik süresi (genelde 1 saat)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Token kullanıldı mı? (tek kullanımlık)
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// Token hangi IP'den talep edildi? (güvenlik logu - opsiyonel)
    /// </summary>
    public string? RequestIp { get; set; }
    
    /// <summary>
    /// Token kullanılma zamanı
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    // Navigation Property
    public Users User { get; set; } = null!;
}
