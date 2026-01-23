using System.Net;
using System.Net.Mail;
using Application.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

/// <summary>
/// SMTP üzerinden email gönderme servisi
/// Gmail, Outlook veya özel SMTP sunucuları ile çalışır
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            // appsettings.json'dan SMTP ayarlarını al
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];

            // SMTP Client oluştur
            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            // Mail mesajı oluştur
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(to);

            // Gönder
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw new InvalidOperationException("Email gönderilemedi. Lütfen daha sonra tekrar deneyin.");
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken, string userName)
    {
        var resetLink = $"{_configuration["AppSettings:FrontendUrl"]}/reset-password?token={resetToken}";
        
        var subject = "Şifre Sıfırlama Talebi";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; margin: 20px 0; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Şifre Sıfırlama</h1>
        </div>
        <div class='content'>
            <p>Merhaba {userName},</p>
            <p>Hesabınız için şifre sıfırlama talebi aldık. Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
            <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
            <p>Veya bu linki tarayıcınıza yapıştırın:</p>
            <p style='word-break: break-all; color: #666;'>{resetLink}</p>
            <p><strong>⏰ Bu link 1 saat geçerlidir.</strong></p>
            <p>Eğer bu talebi siz yapmadıysanız, bu email'i görmezden gelebilirsiniz.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} Social Network. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendEmailVerificationAsync(string to, string verificationToken, string userName)
    {
        var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={verificationToken}";
        
        var subject = "Email Adresinizi Doğrulayın";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; margin: 20px 0; background-color: #2196F3; color: white; text-decoration: none; border-radius: 4px; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Hoş Geldiniz!</h1>
        </div>
        <div class='content'>
            <p>Merhaba {userName},</p>
            <p>Social Network'e hoş geldiniz! Hesabınızı aktifleştirmek için email adresinizi doğrulamanız gerekmektedir.</p>
            <p>Email adresinizi doğrulamak için aşağıdaki butona tıklayın:</p>
            <a href='{verificationLink}' class='button'>Email'imi Doğrula</a>
            <p>Veya bu linki tarayıcınıza yapıştırın:</p>
            <p style='word-break: break-all; color: #666;'>{verificationLink}</p>
            <p><strong>✅ Doğrulama sonrası tüm özellikleri kullanabilirsiniz.</strong></p>
            <p>Eğer bu hesabı siz oluşturmadıysanız, bu email'i görmezden gelebilirsiniz.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} Social Network. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, isHtml: true);
    }
}
