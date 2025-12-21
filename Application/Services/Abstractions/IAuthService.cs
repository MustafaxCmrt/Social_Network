using Application.DTOs.Auth;

namespace Application.Services.Abstractions;

/// <summary>
/// Authentication (Kimlik doğrulama) işlemlerini yöneten interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Kullanıcı kayıt işlemini gerçekleştirir
    /// </summary>
    /// <param name="request">Kayıt bilgileri (ad, soyad, username, email, şifre)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kayıt başarılıysa kullanıcı bilgileri, değilse null</returns>
    Task<RegisterResponseDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı giriş işlemini gerçekleştirir
    /// </summary>
    /// <param name="request">Login bilgileri (username/email ve şifre)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Login başarılıysa JWT token ve kullanıcı bilgileri, değilse null</returns>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refresh token ile yeni access token alır
    /// </summary>
    /// <param name="request">Refresh token bilgisi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yeni access token ve refresh token, refresh token geçersizse null</returns>
    Task<RefreshTokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcının tüm tokenlarını geçersiz kılar (logout)
    /// </summary>
    /// <param name="userId">Çıkış yapacak kullanıcının ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Çıkış sonucu, kullanıcı bulunamazsa null</returns>
    Task<LogoutResponseDto?> LogoutAsync(int userId, CancellationToken cancellationToken = default);
}
