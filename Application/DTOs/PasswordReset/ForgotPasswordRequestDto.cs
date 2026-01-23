namespace Application.DTOs.PasswordReset;

/// <summary>
/// Şifre sıfırlama talebi (kullanıcı email'ini girer)
/// </summary>
public record ForgotPasswordRequestDto
{
    /// <summary>
    /// Şifreyi sıfırlamak isteyen kullanıcının email adresi
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
