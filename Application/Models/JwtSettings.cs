namespace Application.Models;

/// <summary>
/// appsettings.json'dan JWT ayarlarını okumak için kullanılan model
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT token'ı imzalamak için kullanılan gizli anahtar
    /// ÖNEMLİ: Production'da bu değer environment variable veya Azure Key Vault'tan alınmalı
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// Token'ı oluşturan (yayınlayan) - genellikle API'nin URL'i
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// Token'ı kullanacak olan (hedef kitle) - genellikle client uygulamanın URL'i
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Token'ın geçerlilik süresi (dakika cinsinden)
    /// Örnek: 60 = 1 saat, 1440 = 24 saat
    /// </summary>
    public int ExpirationMinutes { get; set; }
    
    /// <summary>
    /// Refresh Token'ın geçerlilik süresi (gün cinsinden)
    /// Örnek: 1 = 1 gün, 7 = 1 hafta, 30 = 1 ay
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; }
}
