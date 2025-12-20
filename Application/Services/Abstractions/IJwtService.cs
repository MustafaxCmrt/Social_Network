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
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(Users user);

    /// <summary>
    /// Token'ın geçerlilik süresini dakika cinsinden döner
    /// </summary>
    /// <returns>Token süresi (dakika)</returns>
    int GetTokenExpirationMinutes();
}
