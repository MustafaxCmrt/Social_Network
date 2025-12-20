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

        // 4. JWT token oluştur
        var accessToken = _jwtService.GenerateAccessToken(user);
        var expiresIn = _jwtService.GetTokenExpirationMinutes();

        // 5. Response DTO oluştur ve döndür
        return new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer",
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}
