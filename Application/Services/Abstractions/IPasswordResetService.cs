using Application.DTOs.PasswordReset;

namespace Application.Services.Abstractions;

/// <summary>
/// Şifre sıfırlama işlemlerini yöneten servis
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Şifre sıfırlama talebi oluşturur ve email gönderir
    /// </summary>
    /// <param name="dto">Kullanıcı email'i</param>
    /// <param name="requestIp">Talep yapan IP adresi (opsiyonel)</param>
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto dto, string? requestIp = null);
    
    /// <summary>
    /// Token ile şifreyi sıfırlar
    /// </summary>
    /// <param name="dto">Token ve yeni şifre</param>
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto dto);
}
