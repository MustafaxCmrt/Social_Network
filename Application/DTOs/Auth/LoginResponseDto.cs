namespace Application.DTOs.Auth;

/// <summary>
/// Login başarılı olduğunda client'a dönen response DTO
/// JWT token bilgilerini içerir
/// Kullanıcı bilgileri (userId, username, email, role) JWT token içinde mevcuttur
/// </summary>
public record LoginResponseDto
{
    /// <summary>
    /// JWT access token - Client bu token'ı her istekte Authorization header'ında gönderir
    /// Token içinde userId, username, email, role bilgileri bulunur
    /// </summary>
    public string AccessToken { get; set; } = null!;
    
    /// <summary>
    /// Refresh token - Access token süresi dolduğunda yeni token almak için kullanılır
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Token'ın geçerlilik süresi (dakika cinsinden)
    /// Örnek: 60 = 1 saat
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token türü - genellikle "Bearer"
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Refresh token'ın geçerlilik süresi (gün cinsinden)
    /// </summary>
    public int RefreshTokenExpiresInDays { get; set; }
}
