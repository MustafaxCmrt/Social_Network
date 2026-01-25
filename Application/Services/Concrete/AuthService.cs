using System.Security.Cryptography;
using System.Text;
using Application.DTOs.Auth;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

/// <summary>
/// Authentication işlemlerini gerçekleştiren servis
/// UnitOfWork pattern ile database işlemlerini yönetir
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IModerationService _moderationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    public AuthService(
        IUnitOfWork unitOfWork, 
        IJwtService jwtService, 
        IModerationService moderationService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _moderationService = moderationService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı kayıt işlemini gerçekleştirir
    /// 1. Username ve Email unique kontrolü
    /// 2. Şifreyi BCrypt ile hashle
    /// 3. Yeni kullanıcı oluştur
    /// 4. Veritabanına kaydet
    /// 5. Response DTO döner
    /// </summary>
    public async Task<RegisterResponseDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Username zaten kullanılıyor mu kontrol et
        var usernameExists = await _unitOfWork.Users.AnyAsync(
            u => u.Username == request.Username && !u.IsDeleted,
            cancellationToken
        );

        if (usernameExists)
            return null; // Username zaten kullanılıyor

        // 2. Email zaten kullanılıyor mu kontrol et
        var emailExists = await _unitOfWork.Users.AnyAsync(
            u => u.Email == request.Email && !u.IsDeleted,
            cancellationToken
        );

        if (emailExists)
            return null; // Email zaten kullanılıyor

        // 3. Şifreyi BCrypt ile hashle
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var plainToken = Guid.NewGuid().ToString(); // Email'de gönderilecek
        var hashedToken = HashToken(plainToken); // DB'de saklanacak

        // 5. Yeni kullanıcı oluştur
        var newUser = new Users
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = Roles.User, // Varsayılan rol: User
            IsActive = true,
            EmailVerified = false, // Henüz doğrulanmadı
            EmailVerificationToken = hashedToken,
            EmailVerificationTokenCreatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 6. Veritabanına kaydet
        try
        {
            await _unitOfWork.Users.CreateAsync(newUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
        {
            // Unique constraint ihlali - username veya email zaten kullanılıyor
            _logger.LogWarning("Duplicate entry hatası: {Message}", ex.InnerException.Message);
            return null;
        }

        // 7. Email doğrulama email'i gönder
        try
        {
            await _emailService.SendEmailVerificationAsync(
                newUser.Email, 
                plainToken, // Düz token email'de
                newUser.FirstName);
        }
        catch (Exception)
        {
            // Email gönderme hatası olsa bile kayıt devam eder
            // Kullanıcı daha sonra yeniden email gönderebilir
        }

        // 6. Response DTO oluştur ve döndür
        return new RegisterResponseDto
        {
            UserId = newUser.Id,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            Username = newUser.Username,
            Email = newUser.Email,
            Role = newUser.Role.ToString()
        };
    }

    /// <summary>
    /// Kullanıcı giriş işlemini gerçekleştirir
    /// 1. Kullanıcıyı email veya username ile bulur
    /// 2. Şifreyi BCrypt ile doğrular
    /// 3. Kullanıcı aktif mi kontrol eder
    /// 4. JWT token oluşturur
    /// 5. Response DTO döner
    /// </summary>
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Kullanıcıyı bul (Email veya Username ile)
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => (u.Email == request.UsernameOrEmail || u.Username == request.UsernameOrEmail)
                 && !u.IsDeleted, // Silinmiş kullanıcıları dahil etme
            cancellationToken
        );

        // Kullanıcı bulunamadı
        if (user == null)
            return null;

        // 2. Şifre kontrolü (BCrypt ile hash karşılaştırması)
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            return null;

        // 3. Kullanıcı aktif mi kontrol et
        if (!user.IsActive)
            return null;

        // 4. Email doğrulandı mı kontrol et
        if (!user.EmailVerified)
        {
            throw new UnauthorizedAccessException(
                "Email adresiniz doğrulanmamış. Lütfen email kutunuzu kontrol edin ve doğrulama linkine tıklayın.");
        }

        // 5. MODERASYON: Kullanıcı ban'lı mı kontrol et
        var (isBanned, activeBan) = await _moderationService.IsUserBannedAsync(user.Id);
        if (isBanned && activeBan != null)
        {
            var banMessage = activeBan.IsPermanent 
                ? "Hesabınız kalıcı olarak yasaklanmıştır." 
                : $"Hesabınız {activeBan.ExpiresAt:dd.MM.yyyy HH:mm} tarihine kadar yasaklanmıştır.";
            banMessage += $" Sebep: {activeBan.Reason}";
            throw new UnauthorizedAccessException(banMessage);
        }

        // 6. Refresh token version'ı artır - hem access hem refresh token aynı versiyonu kullanacak
        user.RefreshTokenVersion++;
        
        // 7. Son giriş zamanını güncelle
        user.LastLoginAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. JWT access token ve refresh token oluştur (aynı version ile)
        var accessToken = _jwtService.GenerateAccessToken(user, user.RefreshTokenVersion);
        var refreshToken = _jwtService.GenerateRefreshToken(user, user.RefreshTokenVersion);
        var expiresIn = _jwtService.GetTokenExpirationMinutes();
        var refreshTokenExpiresInDays = _jwtService.GetRefreshTokenExpirationDays();

        // 9. Response DTO oluştur ve döndür
        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer",
            RefreshTokenExpiresInDays = refreshTokenExpiresInDays
        };
    }
    
    /// <summary>
    /// Refresh token ile yeni access token ve refresh token alır
    /// 1. Refresh token'ı validate eder ve versiyon bilgisini alır
    /// 2. Kullanıcıyı bulur ve versiyon eşleşmesini kontrol eder
    /// 3. Yeni versiyon ile yeni tokenlar oluşturur
    /// </summary>
    public async Task<RefreshTokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Refresh token'ı validate et ve içindeki bilgileri al
        var (isValid, userId, version) = _jwtService.ValidateRefreshToken(request.RefreshToken);
        
        if (!isValid)
            return null;
        
        // 2. Kullanıcıyı bul
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        
        if (user == null || !user.IsActive || user.IsDeleted)
            return null;
        
        // 3. Version kontrolü - token'daki version ile veritabanındaki eşleşmeli
        if (user.RefreshTokenVersion != version)
            return null;
        
        // 4. Yeni version oluştur ve son giriş zamanını güncelle
        user.RefreshTokenVersion++;
        user.LastLoginAt = DateTime.UtcNow; // Refresh token kullanımı da aktif kullanım sayılır
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 5. Yeni access token ve refresh token oluştur (yeni version ile)
        var newAccessToken = _jwtService.GenerateAccessToken(user, user.RefreshTokenVersion);
        var newRefreshToken = _jwtService.GenerateRefreshToken(user, user.RefreshTokenVersion);
        var expiresIn = _jwtService.GetTokenExpirationMinutes();
        var refreshTokenExpiresInDays = _jwtService.GetRefreshTokenExpirationDays();
        
        // 6. Response DTO oluştur ve döndür
        return new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer",
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresInDays = refreshTokenExpiresInDays
        };
    }
    
    /// <summary>
    /// Kullanıcının tüm tokenlarını geçersiz kılar
    /// Token version'ı artırır - böylece mevcut tüm tokenlar geçersiz olur
    /// </summary>
    public async Task<LogoutResponseDto?> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        // 1. Kullanıcıyı bul
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        
        if (user == null || !user.IsActive || user.IsDeleted)
            return null;
        
        // 2. Token version'ı artır - tüm mevcut tokenlar geçersiz olur
        user.RefreshTokenVersion++;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 3. Başarılı çıkış mesajı döndür
        return new LogoutResponseDto
        {
            Message = "Başarıyla çıkış yapıldı"
        };
    }

    /// <summary>
    /// Email doğrulama email'ini tekrar gönderir
    /// Rate limiting: 1 email / 2 dakika
    /// </summary>
    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        // 1. Kullanıcıyı bul
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        // Kullanıcı bulunamadı (güvenlik: her zaman true döner)
        if (user == null)
            return true; // Email leak prevention

        // 2. Zaten doğrulanmış mı?
        if (user.EmailVerified)
        {
            throw new InvalidOperationException("Email adresi zaten doğrulanmış.");
        }

        // 3. Rate limiting: Son 2 dakikada email gönderildi mi?
        if (user.EmailVerificationTokenCreatedAt.HasValue &&
            user.EmailVerificationTokenCreatedAt.Value > DateTime.UtcNow.AddMinutes(-2))
        {
            var waitTime = 2 - (DateTime.UtcNow - user.EmailVerificationTokenCreatedAt.Value).TotalMinutes;
            throw new InvalidOperationException(
                $"Çok fazla email gönderme talebi. Lütfen {Math.Ceiling(waitTime)} dakika bekleyin.");
        }

        // 4. Yeni token oluştur
        var plainToken = Guid.NewGuid().ToString();
        var hashedToken = HashToken(plainToken);

        // 5. Token'ı güncelle
        user.EmailVerificationToken = hashedToken;
        user.EmailVerificationTokenCreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // 6. Email gönder
        try
        {
            await _emailService.SendEmailVerificationAsync(user.Email, plainToken, user.FirstName);
            return true;
        }
        catch (Exception)
        {
            // Email gönderme hatası
            throw new InvalidOperationException("Email gönderilemedi. Lütfen daha sonra tekrar deneyin.");
        }
    }

    /// <summary>
    /// Token'ı SHA256 ile hash'ler (Email verification için)
    /// </summary>
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash); // 64 karakter hex string
    }
}
