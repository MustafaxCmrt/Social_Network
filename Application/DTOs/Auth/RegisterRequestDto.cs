namespace Application.DTOs.Auth;

/// <summary>
/// Kullanıcı kayıt isteği için kullanılan DTO
/// Client'tan gelen yeni kullanıcı bilgilerini içerir
/// </summary>
public record RegisterRequestDto
{
    /// <summary>
    /// Kullanıcının adı
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Kullanıcının soyadı
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Kullanıcı adı (unique olmalı)
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Email adresi (unique olmalı)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Şifre (plain text olarak gelir, sunucuda hash'lenir)
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Şifre tekrarı (Password ile aynı olmalı)
    /// </summary>
    public string ConfirmPassword { get; set; } = null!;
}
