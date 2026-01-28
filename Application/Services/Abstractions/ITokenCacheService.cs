namespace Application.Services.Abstractions;

/// <summary>
/// Token version cache yönetimi için servis
/// RefreshTokenVersion değiştiğinde cache'i invalidate eder
/// </summary>
public interface ITokenCacheService
{
    /// <summary>
    /// Kullanıcının token version cache'ini temizler
    /// Email/şifre değişikliği gibi durumlarda çağrılır
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    void InvalidateUserTokenCache(int userId);
}
