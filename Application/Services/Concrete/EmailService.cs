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

    public async Task SendEmailChangeNotificationAsync(string to, string userName, string newEmail, bool changedByAdmin = false)
    {
        var subject = "Email Adresiniz Değiştirildi";
        var changedByText = changedByAdmin 
            ? "Hesabınızın email adresi <strong>sistem yöneticisi (admin) tarafından</strong> değiştirildi." 
            : "Hesabınızın email adresi değiştirildi.";
        var securityNote = changedByAdmin
            ? "Eğer bu değişikliği talep etmediyseniz, <strong>derhal</strong> destek ekibimizle iletişime geçin ve hesabınızın güvenliğini kontrol edin."
            : "Eğer bu değişikliği siz yapmadıysanız, hesabınız tehlikede olabilir! <strong>Derhal</strong> destek ekibimizle iletişime geçin.";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #FF9800; padding: 12px; margin: 20px 0; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Email Değişikliği</h1>
        </div>
        <div class='content'>
            <p>Merhaba {userName},</p>
            <p>{changedByText}</p>
            <div class='warning'>
                <p><strong>Eski Email:</strong> {to}</p>
                <p><strong>Yeni Email:</strong> {newEmail}</p>
                {(changedByAdmin ? "<p><strong>Değiştiren:</strong> Sistem Yöneticisi (Admin)</p>" : "")}
            </div>
            <p><strong>⚠️ Önemli Güvenlik Bilgisi:</strong></p>
            <ul>
                <li>Yeni email adresi doğrulanana kadar hesabınıza giriş yapamazsınız</li>
                <li>Tüm aktif oturumlarınız sonlandırılmıştır</li>
                <li>Yeni email adresinize doğrulama linki gönderilmiştir</li>
            </ul>
            <p>{securityNote}</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} Social Network. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, isHtml: true);
    }

    public async Task SendEmailChangeVerificationAsync(string to, string verificationToken, string userName)
    {
        var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={verificationToken}";
        
        var subject = "Yeni Email Adresinizi Doğrulayın";
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
        .info {{ background-color: #d1ecf1; border-left: 4px solid #2196F3; padding: 12px; margin: 20px 0; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Email Doğrulama</h1>
        </div>
        <div class='content'>
            <p>Merhaba {userName},</p>
            <p>Email adresinizi değiştirdiniz. Yeni email adresinizi doğrulamak için aşağıdaki butona tıklayın:</p>
            <a href='{verificationLink}' class='button'>Yeni Email'imi Doğrula</a>
            <p>Veya bu linki tarayıcınıza yapıştırın:</p>
            <p style='word-break: break-all; color: #666;'>{verificationLink}</p>
            <div class='info'>
                <p><strong>ℹ️ Önemli:</strong></p>
                <ul style='margin: 0; padding-left: 20px;'>
                    <li>Email doğrulaması tamamlanana kadar hesabınıza giriş yapamazsınız</li>
                    <li>Tüm aktif oturumlarınız güvenlik nedeniyle sonlandırılmıştır</li>
                    <li>Doğrulama sonrası yeni email adresinizle giriş yapabilirsiniz</li>
                </ul>
            </div>
            <p>Eğer bu değişikliği siz yapmadıysanız, bu email'i görmezden gelebilir ve eski email adresinizle giriş yapmaya devam edebilirsiniz.</p>
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
