using Domain.Entities;

namespace Application.Services.Abstractions;

/// <summary>
/// JWT token oluşturma ve doğrulama işlemlerini yöneten interface
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Kullanıcı bilgilerine göre JWT access token oluşturur
    /// </summary>
    /// <param name="user">Token oluşturulacak kullanıcı</param>
    /// <param name="version">Token versiyonu - kullanıcının RefreshTokenVersion değeri</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(Users user, int version);

    /// <summary>
    /// Kullanıcı için refresh token oluşturur
    /// </summary>
    /// <param name="user">Token oluşturulacak kullanıcı</param>
    /// <param name="version">Token versiyonu - kullanıcının RefreshTokenVersion değeri</param>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken(Users user, int version);
    
    /// <summary>
    /// Token'ın geçerlilik süresini dakika cinsinden döner
    /// </summary>
    /// <returns>Token süresi (dakika)</returns>
    int GetTokenExpirationMinutes();
    
    /// <summary>
    /// Refresh token'ın geçerlilik süresini gün cinsinden döner
    /// </summary>
    /// <returns>Token süresi (gün)</returns>
    int GetRefreshTokenExpirationDays();
    
    /// <summary>
    /// Refresh token'ı validate eder ve içindeki bilgileri döner
    /// </summary>
    /// <param name="refreshToken">Validate edilecek refresh token</param>
    /// <returns>Token geçerli mi, kullanıcı ID'si ve token versiyonu</returns>
    (bool isValid, int userId, int version) ValidateRefreshToken(string refreshToken);
    
    /// <summary>
    /// Access token'ı validate eder ve içindeki bilgileri döner
    /// </summary>
    /// <param name="accessToken">Validate edilecek access token</param>
    /// <returns>Token geçerli mi, kullanıcı ID'si ve token versiyonu</returns>
    (bool isValid, int userId, int version) ValidateAccessToken(string accessToken);
}
