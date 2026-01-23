using Application.DTOs.PasswordReset;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Şifre sıfırlama işlemleri için controller
/// </summary>
public class PasswordResetController : AppController
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IPasswordResetService passwordResetService,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator,
        ILogger<PasswordResetController> logger)
    {
        _passwordResetService = passwordResetService;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _logger = logger;
    }

    /// <summary>
    /// Şifre sıfırlama talebi oluşturur ve email gönderir
    /// </summary>
    /// <param name="dto">Kullanıcı email adresi</param>
    /// <returns>Başarılı mesajı (her zaman true döner - güvenlik)</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            await _passwordResetService.ForgotPasswordAsync(dto, requestIp);

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
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
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
                _logger.LogInformation("Password reset successful for token");
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
}
