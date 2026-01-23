using System.Security.Cryptography;
using System.Text;
using Application.DTOs.PasswordReset;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Şifre sıfırlama servisi
/// Token oluşturma, doğrulama ve şifre güncelleme işlemlerini yapar
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<PasswordResetService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto dto, string? requestIp = null)
    {
        try
        {
            // 1️⃣ Kullanıcıyı bul
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                u => u.Email == dto.Email && !u.IsDeleted);

            // Kullanıcı bulunamadı → GÜVENLİK: Her zaman true döner (email leak prevention)
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                return true; // Bilgi sızdırma önlemek için true dön
            }

            // 2️⃣ Rate limiting kontrolü (10 dakikada 1 token)
            var recentToken = await _unitOfWork.PasswordResetTokens.FirstOrDefaultAsync(
                t => t.UserId == user.Id 
                     && t.CreatedAt > DateTime.UtcNow.AddMinutes(-10));

            if (recentToken != null)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}", user.Id);
                throw new InvalidOperationException("Çok fazla şifre sıfırlama talebi gönderdiniz. Lütfen 10 dakika bekleyin.");
            }

            // 3️⃣ Eski tokenları pasif yap (tek aktif token olsun)
            var oldTokens = await _unitOfWork.PasswordResetTokens.FindAsync(
                t => t.UserId == user.Id && !t.IsUsed);
            
            foreach (var oldToken in oldTokens)
            {
                oldToken.IsUsed = true; // Eski tokenları invalidate et
            }

            // 4️⃣ Yeni token oluştur (GUID - tahmin edilemez)
            var plainToken = Guid.NewGuid().ToString(); // Email'de gönderilecek
            var hashedToken = HashToken(plainToken); // DB'de saklanacak (SHA256)

            var resetToken = new PasswordResetTokens
            {
                UserId = user.Id,
                Guid = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 saat geçerli
                IsUsed = false,
                RequestIp = requestIp,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PasswordResetTokens.CreateAsync(resetToken);
            await _unitOfWork.SaveChangesAsync();

            // 5️⃣ Email gönder (düz token ile)
            await _emailService.SendPasswordResetEmailAsync(
                user.Email, 
                plainToken, 
                user.FirstName ?? user.Username);

            _logger.LogInformation("Password reset token created for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPasswordAsync for email {Email}", dto.Email);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        try
        {
            // 1️⃣ Token'ı hash'le (DB'de hash olarak saklıyoruz)
            var hashedToken = HashToken(dto.Token);

            // 2️⃣ Token'ı bul
            var resetToken = await _unitOfWork.PasswordResetTokens.FirstOrDefaultAsync(
                t => t.Guid == hashedToken && !t.IsUsed);

            if (resetToken == null)
            {
                _logger.LogWarning("Invalid or used token: {Token}", dto.Token);
                throw new InvalidOperationException("Geçersiz veya kullanılmış token.");
            }

            // 3️⃣ Token süresi dolmuş mu?
            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired token for user {UserId}", resetToken.UserId);
                throw new InvalidOperationException("Token süresi dolmuş. Lütfen yeni bir şifre sıfırlama talebi oluşturun.");
            }

            // 4️⃣ Kullanıcıyı bul
            var user = await _unitOfWork.Users.GetByIdAsync(resetToken.UserId);
            if (user == null)
            {
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            }

            // 5️⃣ Şifreyi güncelle (BCrypt ile hash'le)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            // 6️⃣ Refresh token versiyonunu artır (tüm cihazlardan çıkış yapsın)
            user.RefreshTokenVersion++;

            _unitOfWork.Users.Update(user);

            // 7️⃣ Token'ı kullanılmış olarak işaretle
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            _unitOfWork.PasswordResetTokens.Update(resetToken);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset successful for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPasswordAsync");
            throw;
        }
    }

    /// <summary>
    /// Token'ı SHA256 ile hash'ler
    /// </summary>
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash); // 64 karakter hex string
    }
}
