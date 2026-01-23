namespace Application.DTOs.PasswordReset;

/// <summary>
/// Şifre sıfırlama işlemi (token + yeni şifre)
/// </summary>
public record ResetPasswordRequestDto
{
    /// <summary>
    /// Email'den gelen reset token (GUID)
    /// </summary>
    public string Token { get; init; } = string.Empty;
    
    /// <summary>
    /// Yeni şifre
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
    
    /// <summary>
    /// Yeni şifre tekrar (doğrulama için)
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;
}
