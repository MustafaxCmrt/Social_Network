namespace Application.DTOs.Auth;

/// <summary>
/// Login isteği için kullanılan DTO
/// Client tarafından gönderilen kullanıcı adı/email ve şifre bilgilerini içerir
/// </summary>
public record LoginRequestDto
{
    /// <summary>
    /// Kullanıcı adı veya email adresi
    /// </summary>
    public string UsernameOrEmail { get; set; } = null!;

    /// <summary>
    /// Kullanıcının şifresi (plain text olarak gelir, sunucuda hash'lenmiş ile karşılaştırılır)
    /// </summary>
    public string Password { get; set; } = null!;
}
