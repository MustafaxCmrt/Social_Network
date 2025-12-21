namespace Application.DTOs.Auth;

/// <summary>
/// Refresh token isteği için kullanılan DTO
/// Access token süresi dolduğunda yeni token almak için kullanılır
/// </summary>
public record RefreshTokenRequestDto
{
    /// <summary>
    /// Kullanıcının refresh token'ı
    /// </summary>
    public string RefreshToken { get; set; } = null!;
}
