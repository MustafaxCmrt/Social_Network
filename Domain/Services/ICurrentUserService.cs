namespace Domain.Services;

/// <summary>
/// Mevcut oturum açmış kullanıcı bilgilerini sağlayan servis
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Mevcut kullanıcının ID'sini döner
    /// </summary>
    /// <returns>Kullanıcı ID'si, oturum açık değilse null</returns>
    int? GetCurrentUserId();

    /// <summary>
    /// Mevcut kullanıcının kullanıcı adını döner
    /// </summary>
    /// <returns>Kullanıcı adı, oturum açık değilse null</returns>
    string? GetCurrentUsername();

    /// <summary>
    /// Mevcut kullanıcının rolünü döner
    /// </summary>
    /// <returns>Rol adı, oturum açık değilse null</returns>
    string? GetCurrentUserRole();

    /// <summary>
    /// Kullanıcının oturum açıp açmadığını kontrol eder
    /// </summary>
    /// <returns>True eğer oturum açıksa, değilse false</returns>
    bool IsAuthenticated();
}
