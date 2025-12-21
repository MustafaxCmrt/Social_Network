namespace Application.DTOs.Auth;

/// <summary>
/// Kullanıcı çıkış yanıtı için DTO
/// </summary>
public class LogoutResponseDto
{
    /// <summary>
    /// Çıkış işlemi sonucu mesajı
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
