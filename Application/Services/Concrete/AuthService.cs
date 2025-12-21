using Application.DTOs.Auth;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Authentication işlemlerini gerçekleştiren servis
/// UnitOfWork pattern ile database işlemlerini yönetir
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
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

        // 4. Yeni kullanıcı oluştur
        var newUser = new Users
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = Roles.User, // Varsayılan rol: User
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 5. Veritabanına kaydet
        await _unitOfWork.Users.CreateAsync(newUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        // 4. Refresh token version'ı artır - hem access hem refresh token aynı versiyonu kullanacak
        user.RefreshTokenVersion++;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. JWT access token ve refresh token oluştur (aynı version ile)
        var accessToken = _jwtService.GenerateAccessToken(user, user.RefreshTokenVersion);
        var refreshToken = _jwtService.GenerateRefreshToken(user, user.RefreshTokenVersion);
        var expiresIn = _jwtService.GetTokenExpirationMinutes();
        var refreshTokenExpiresInDays = _jwtService.GetRefreshTokenExpirationDays();

        // 6. Response DTO oluştur ve döndür
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
        
        // 4. Yeni version oluştur
        user.RefreshTokenVersion++;
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
}
