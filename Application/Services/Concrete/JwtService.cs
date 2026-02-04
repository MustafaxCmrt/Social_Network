using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Models;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Concrete;

/// <summary>
/// JWT token oluşturma ve yönetme işlemlerini gerçekleştiren servis
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        
        // SORUN BULMA: JWT Settings değerlerini logla
        _logger.LogInformation("JWT Settings loaded - ExpirationMinutes: {ExpirationMinutes}, RefreshTokenExpirationDays: {RefreshTokenExpirationDays}", 
            _jwtSettings.ExpirationMinutes, 
            _jwtSettings.RefreshTokenExpirationDays);
    }

    /// <summary>
    /// Kullanıcı bilgilerine göre JWT access token oluşturur
    /// Token içinde kullanıcı ID, email, username, role ve version bilgileri bulunur
    /// </summary>
    public string GenerateAccessToken(Users user, int version)
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
            new Claim("UserId", user.Id.ToString()),
            
            // Token Version - Versiyonlama için
            new Claim("TokenVersion", version.ToString()),
            
            // Token Type - Access token olduğunu belirtir
            new Claim("TokenType", "access")
        };

        // Secret Key'i byte array'e çevir
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        // İmzalama algoritması (HMAC SHA-256)
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Token süresini hesapla ve logla
        var expirationTime = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
        _logger.LogInformation("Creating Access Token - UserId: {UserId}, ExpirationMinutes: {ExpirationMinutes}, ExpiresAt: {ExpiresAt}", 
            user.Id, 
            _jwtSettings.ExpirationMinutes, 
            expirationTime);

        // Token'ı oluştur
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,                                    // Token'ı kim oluşturdu
            audience: _jwtSettings.Audience,                                // Token'ı kim kullanacak
            claims: claims,                                                  // Kullanıcı bilgileri
            expires: expirationTime,                                         // Geçerlilik süresi
            signingCredentials: credentials                                  // İmza
        );

        // Token'ı string'e çevir ve döndür
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    /// <summary>
    /// Kullanıcı için JWT refresh token oluşturur
    /// Refresh token içinde kullanıcı ID, version ve token type bilgileri bulunur
    /// </summary>
    public string GenerateRefreshToken(Users user, int version)
    {
        var claims = new List<Claim>
        {
            // JTI (JWT ID) - Token'ın benzersiz ID'si
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            
            // Sub (Subject) - Kullanıcı ID'si
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            
            // Token Version - Versiyonlama için
            new Claim("TokenVersion", version.ToString()),
            
            // Token Type - Refresh token olduğunu belirtir
            new Claim("TokenType", "refresh")
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Token'ın geçerlilik süresini dakika cinsinden döner
    /// </summary>
    public int GetTokenExpirationMinutes()
    {
        return _jwtSettings.ExpirationMinutes;
    }
    
    /// <summary>
    /// Rastgele ve güvenli bir refresh token oluşturur
    /// Cryptographic random number generator kullanır
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    /// <summary>
    /// Refresh token'ın geçerlilik süresini gün cinsinden döner
    /// </summary>
    public int GetRefreshTokenExpirationDays()
    {
        return _jwtSettings.RefreshTokenExpirationDays;
    }
    
    /// <summary>
    /// Refresh token'ı validate eder ve içindeki bilgileri döner
    /// </summary>
    public (bool isValid, int userId, int version) ValidateRefreshToken(string refreshToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);

            // Sub claim'ini farklı yollarla dene
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? principal.FindFirst("sub")?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var versionClaim = principal.FindFirst("TokenVersion")?.Value;
            var tokenTypeClaim = principal.FindFirst("TokenType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || 
                string.IsNullOrEmpty(versionClaim) || 
                tokenTypeClaim != "refresh")
            {
                return (false, 0, 0);
            }

            return (true, int.Parse(userIdClaim), int.Parse(versionClaim));
        }
        catch
        {
            return (false, 0, 0);
        }
    }
    
    /// <summary>
    /// Access token'ı validate eder ve içindeki bilgileri döner
    /// </summary>
    public (bool isValid, int userId, int version) ValidateAccessToken(string accessToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var versionClaim = principal.FindFirst("TokenVersion")?.Value;
            var tokenTypeClaim = principal.FindFirst("TokenType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || 
                string.IsNullOrEmpty(versionClaim) || 
                tokenTypeClaim != "access")
            {
                return (false, 0, 0);
            }

            return (true, int.Parse(userIdClaim), int.Parse(versionClaim));
        }
        catch
        {
            return (false, 0, 0);
        }
    }
}
