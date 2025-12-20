using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Models;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Concrete;

/// <summary>
/// JWT token oluşturma ve yönetme işlemlerini gerçekleştiren servis
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Kullanıcı bilgilerine göre JWT token oluşturur
    /// Token içinde kullanıcı ID, email, username ve role bilgileri bulunur
    /// </summary>
    public string GenerateAccessToken(Users user)
    {
        // Token'a eklenecek claims (kullanıcı bilgileri)
        var claims = new List<Claim>
        {
            // JTI (JWT ID) - Token'ın benzersiz ID'si
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // Sub (Subject) - Kullanıcı ID'si
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // Email
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // Username - ClaimTypes.Name kullanarak User.Identity.Name ile erişilebilir
            new Claim(ClaimTypes.Name, user.Username),

            // Role - [Authorize(Roles = "Admin")] gibi kullanılabilir
            new Claim(ClaimTypes.Role, user.Role.ToString()),

            // User ID - Custom claim
            new Claim("UserId", user.Id.ToString())
        };

        // Secret Key'i byte array'e çevir
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        // İmzalama algoritması (HMAC SHA-256)
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Token'ı oluştur
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,                                    // Token'ı kim oluşturdu
            audience: _jwtSettings.Audience,                                // Token'ı kim kullanacak
            claims: claims,                                                  // Kullanıcı bilgileri
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes), // Geçerlilik süresi
            signingCredentials: credentials                                  // İmza
        );

        // Token'ı string'e çevir ve döndür
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Token'ın geçerlilik süresini dakika cinsinden döner
    /// </summary>
    public int GetTokenExpirationMinutes()
    {
        return _jwtSettings.ExpirationMinutes;
    }
}
