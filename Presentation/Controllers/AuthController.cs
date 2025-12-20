using Application.DTOs.Auth;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Authentication (Kimlik doğrulama) işlemleri için API controller
/// Login, Register gibi işlemleri yönetir
/// </summary>
public class AuthController : AppController
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
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
    /// Kullanıcı kayıt endpoint'i (ileride eklenecek)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        // TODO: Register işlemi eklenecek
        return Ok(new { Message = "Register endpoint - Yakında eklenecek" });
    }
}
