using Application.DTOs.Auth;
using Application.DTOs.PasswordReset;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence.UnitOfWork;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Authentication (Kimlik doğrulama) işlemleri için API controller
/// Login, Register, Password Reset gibi işlemleri yönetir
/// </summary>
public class AuthController : AppController
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;
    private readonly IValidator<ResendVerificationEmailDto> _resendVerificationValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService,
        IUnitOfWork unitOfWork,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator,
        IValidator<ResendVerificationEmailDto> resendVerificationValidator,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _unitOfWork = unitOfWork;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _resendVerificationValidator = resendVerificationValidator;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı giriş endpoint'i
    /// </summary>
    /// <param name="request">Login bilgileri (username/email ve şifre)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Login başarılı, JWT token döner
    /// 400 Bad Request - Validation hatası
    /// 401 Unauthorized - Kullanıcı adı/şifre hatalı veya kullanıcı aktif değil
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        // 1. Validation (FluentValidation ile otomatik yapılır ama manuel de kontrol edebiliriz)
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

        // 2. Login servisini çağır
        var result = await _authService.LoginAsync(request, cancellationToken);

        // 3. Login başarısız (kullanıcı bulunamadı, şifre hatalı veya aktif değil)
        if (result == null)
        {
            return Unauthorized(new
            {
                Message = "Kullanıcı adı/email veya şifre hatalı ya da hesabınız aktif değil"
            });
        }

        // 4. Login başarılı - JWT token döndür
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı kayıt endpoint'i
    /// </summary>
    /// <param name="request">Kayıt bilgileri (ad, soyad, username, email, şifre)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 201 Created - Kayıt başarılı, kullanıcı bilgileri döner
    /// 400 Bad Request - Validation hatası
    /// 409 Conflict - Username veya Email zaten kullanılıyor
    /// </returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        // 1. Validation
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

        // 2. Register servisini çağır
        var result = await _authService.RegisterAsync(request, cancellationToken);

        // 3. Kayıt başarısız (Username veya Email zaten kullanılıyor)
        if (result == null)
        {
            return Conflict(new
            {
                Message = "Kullanıcı adı veya email adresi zaten kullanılıyor"
            });
        }

        // 4. Kayıt başarılı - 201 Created döndür
        return CreatedAtAction(
            nameof(Register),
            new { id = result.UserId },
            result
        );
    }
    
    /// <summary>
    /// Refresh token ile yeni access token alma endpoint'i
    /// </summary>
    /// <param name="request">Refresh token bilgisi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Yeni access token ve refresh token döner
    /// 401 Unauthorized - Refresh token geçersiz veya süresi dolmuş
    /// </returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        // 1. Refresh token servisini çağır
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        
        // 2. Refresh token geçersiz veya süresi dolmuş
        if (result == null)
        {
            return Unauthorized(new
            {
                Message = "Refresh token geçersiz veya süresi dolmuş"
            });
        }
        
        // 3. Yeni token'ları döndür
        return Ok(result);
    }
    
    /// <summary>
    /// Kullanıcı çıkış (logout) endpoint'i
    /// Authorization header'daki token'dan userId alır ve token version'ını artırarak tüm tokenları geçersiz kılar
    /// </summary>
    /// <returns>
    /// 200 OK - Çıkış başarılı
    /// 401 Unauthorized - Token geçersiz veya kullanıcı bulunamadı
    /// </returns>
    [HttpPost("logout")]
    [Authorize] // JWT authentication gerekli
    [ProducesResponseType(typeof(LogoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        // Token'dan userId'yi al
        var userIdClaim = User.FindFirst("UserId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Geçersiz token" });

        var result = await _authService.LogoutAsync(userId, HttpContext.RequestAborted);

        if (result == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı" });

        return Ok(result);
    }
    /// <summary>
    /// Şifre sıfırlama talebi oluşturur ve email gönderir
    /// </summary>
    /// <param name="dto">Kullanıcı email adresi</param>
    /// <returns>Başarılı mesajı (her zaman true döner - güvenlik)</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        // Validation
        var validationResult = await _forgotPasswordValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        // IP adresini al (güvenlik logu için)
        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            await _passwordResetService.ForgotPasswordAsync(dto, requestIp);

            // Her zaman aynı mesaj (email leak prevention)
            _logger.LogInformation("Password reset requested for email: {Email}", dto.Email);
            return Ok(new 
            { 
                message = "Eğer bu email adresi sistemde kayıtlıysa, şifre sıfırlama linki gönderilmiştir. Lütfen email kutunuzu kontrol edin." 
            });
        }
        catch (InvalidOperationException ex)
        {
            // Rate limiting hatası
            _logger.LogWarning(ex, "Rate limit exceeded for forgot password");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Email gönderme hatası
            _logger.LogError(ex, "Error in ForgotPassword for email: {Email}", dto.Email);
            return StatusCode(500, new { message = "Şifre sıfırlama talebi işlenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
        }
    }

    /// <summary>
    /// Token ile şifreyi sıfırlar
    /// </summary>
    /// <param name="dto">Token ve yeni şifre bilgileri</param>
    /// <returns>Başarılı veya hata mesajı</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        // Validation
        var validationResult = await _resetPasswordValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        try
        {
            var result = await _passwordResetService.ResetPasswordAsync(dto);

            if (result)
            {
                _logger.LogInformation("Password reset successful");
                return Ok(new { message = "Şifreniz başarıyla sıfırlandı. Artık yeni şifrenizle giriş yapabilirsiniz." });
            }

            return BadRequest(new { message = "Şifre sıfırlama başarısız oldu." });
        }
        catch (InvalidOperationException ex)
        {
            // Token hatası (geçersiz, kullanılmış, süresi dolmuş)
            _logger.LogWarning(ex, "Invalid token in ResetPassword");
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            // Kullanıcı bulunamadı
            _logger.LogWarning(ex, "User not found in ResetPassword");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Beklenmeyen hata
            _logger.LogError(ex, "Error in ResetPassword");
            return StatusCode(500, new { message = "Şifre sıfırlanırken bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
        }
    }
    /// <summary>
    /// Email doğrulama (kayıt sonrası)
    /// </summary>
    /// <param name="token">Email'den gelen verification token</param>
    /// <returns>Başarılı veya hata mesajı</returns>
    [HttpGet("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Token gereklidir." });
        }

        try
        {
            _logger.LogInformation("Email verification attempt with token: {Token}", token);
            
            // Token'ı hash'le (DB'de hash olarak saklıyoruz)
            var hashedToken = HashToken(token);
            _logger.LogInformation("Hashed token: {HashedToken}", hashedToken);

            // Kullanıcıyı token ile bul
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                u => u.EmailVerificationToken == hashedToken && !u.EmailVerified);

            if (user == null)
            {
                _logger.LogWarning("Invalid or already verified token. Hashed token: {HashedToken}", hashedToken);
                
                // Debug: Tüm doğrulanmamış kullanıcıları kontrol et
                var unverifiedUsers = await _unitOfWork.Users.FindAsync(u => !u.EmailVerified);
                _logger.LogInformation("Unverified users count: {Count}", unverifiedUsers.Count());
                foreach (var u in unverifiedUsers)
                {
                    _logger.LogInformation("User {UserId} - Token in DB: {DbToken}", u.Id, u.EmailVerificationToken);
                }
                
                return BadRequest(new { message = "Geçersiz veya zaten doğrulanmış token." });
            }

            // Email'i doğrula
            user.EmailVerified = true;
            user.EmailVerificationToken = null; // Token'ı temizle
            user.EmailVerificationTokenCreatedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
            return Ok(new 
            { 
                message = "Email adresiniz başarıyla doğrulandı! Güvenlik nedeniyle lütfen tekrar giriş yapın.",
                requiresRelogin = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyEmail");
            return StatusCode(500, new { message = "Email doğrulanırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Email doğrulama email'ini tekrar gönderir
    /// Rate limiting: 2 dakikada 1 email
    /// </summary>
    /// <param name="request">Kullanıcının email adresi</param>
    /// <returns>Başarı mesajı (güvenlik nedeniyle her zaman aynı mesaj)</returns>
    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailDto request)
    {
        try
        {
            // 1. Validasyon
            var validationResult = await _resendVerificationValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return BadRequest(new { errors });
            }

            // 2. Service çağrısı
            await _authService.ResendVerificationEmailAsync(request.Email);

            // 3. Her durumda aynı mesaj (email leak prevention)
            _logger.LogInformation("Resend verification email requested for {Email}", request.Email);
            return Ok(new
            {
                message = "Eğer bu email adresi kayıtlıysa ve doğrulanmamışsa, doğrulama email'i gönderildi."
            });
        }
        catch (InvalidOperationException ex)
        {
            // Rate limiting veya zaten doğrulanmış hatası
            _logger.LogWarning(ex, "Resend verification failed for {Email}", request.Email);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResendVerificationEmail");
            return StatusCode(500, new { message = "Email gönderilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Token'ı SHA256 ile hash'ler
    /// </summary>
    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
