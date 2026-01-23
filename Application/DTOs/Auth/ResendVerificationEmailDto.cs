namespace Application.DTOs.Auth;

/// <summary>
/// Email doğrulama tekrar gönderme talebi
/// </summary>
public record ResendVerificationEmailDto
{
    /// <summary>
    /// Kullanıcının email adresi
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
