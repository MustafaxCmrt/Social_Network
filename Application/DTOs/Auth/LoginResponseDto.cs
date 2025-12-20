namespace Application.DTOs.Auth;

/// <summary>
/// Login başarılı olduğunda client'a dönen response DTO
/// JWT token ve kullanıcı bilgilerini içerir
/// </summary>
public record LoginResponseDto
{
    /// <summary>
    /// JWT access token - Client bu token'ı her istekte Authorization header'ında gönderir
    /// </summary>
    public string AccessToken { get; set; } = null!;

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
    /// Giriş yapan kullanıcının ID'si
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Email adresi
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Kullanıcının rolü (Admin, User, Moderator vs.)
    /// </summary>
    public string Role { get; set; } = null!;
}
