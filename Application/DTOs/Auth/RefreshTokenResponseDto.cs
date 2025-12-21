namespace Application.DTOs.Auth;

/// <summary>
/// Refresh token başarılı olduğunda dönen response
/// Yeni access token ve refresh token içerir
/// </summary>
public record RefreshTokenResponseDto
{
    /// <summary>
    /// Yeni JWT access token
    /// </summary>
    public string AccessToken { get; set; } = null!;
    
    /// <summary>
    /// Token'ın geçerlilik süresi (dakika cinsinden)
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// Token türü - genellikle "Bearer"
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Yeni refresh token
    /// </summary>
    public string RefreshToken { get; set; } = null!;
    
    /// <summary>
    /// Refresh token'ın geçerlilik süresi (gün cinsinden)
    /// </summary>
    public int RefreshTokenExpiresInDays { get; set; }
}
