using Application.DTOs.Auth;

namespace Application.Services.Abstractions;

/// <summary>
/// Authentication (Kimlik doğrulama) işlemlerini yöneten interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Kullanıcı giriş işlemini gerçekleştirir
    /// </summary>
    /// <param name="request">Login bilgileri (username/email ve şifre)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Login başarılıysa JWT token ve kullanıcı bilgileri, değilse null</returns>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
