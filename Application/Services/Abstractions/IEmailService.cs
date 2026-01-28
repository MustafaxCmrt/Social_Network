namespace Application.Services.Abstractions;

/// <summary>
/// Email gönderme servisi
/// SMTP üzerinden email gönderir
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Tek bir alıcıya email gönderir
    /// </summary>
    /// <param name="to">Alıcı email adresi</param>
    /// <param name="subject">Email konusu</param>
    /// <param name="body">Email içeriği (HTML olabilir)</param>
    /// <param name="isHtml">İçerik HTML mi?</param>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    
    /// <summary>
    /// Şifre sıfırlama email'i gönderir (özel template)
    /// </summary>
    /// <param name="to">Kullanıcı email'i</param>
    /// <param name="resetToken">Reset token (düz metin GUID)</param>
    /// <param name="userName">Kullanıcı adı</param>
    Task SendPasswordResetEmailAsync(string to, string resetToken, string userName);
    
    /// <summary>
    /// Email doğrulama email'i gönderir (kayıt sonrası)
    /// </summary>
    /// <param name="to">Kullanıcı email'i</param>
    /// <param name="verificationToken">Verification token (düz metin GUID)</param>
    /// <param name="userName">Kullanıcı adı</param>
    Task SendEmailVerificationAsync(string to, string verificationToken, string userName);
    
    /// <summary>
    /// Email değişikliği bildirim email'i gönderir (eski email'e)
    /// </summary>
    /// <param name="to">Eski email adresi</param>
    /// <param name="userName">Kullanıcı adı</param>
    /// <param name="newEmail">Yeni email adresi</param>
    /// <param name="changedByAdmin">Admin tarafından mı değiştirildi?</param>
    Task SendEmailChangeNotificationAsync(string to, string userName, string newEmail, bool changedByAdmin = false);
    
    /// <summary>
    /// Email değişikliği doğrulama email'i gönderir (yeni email'e)
    /// </summary>
    /// <param name="to">Yeni email adresi</param>
    /// <param name="verificationToken">Verification token (düz metin GUID)</param>
    /// <param name="userName">Kullanıcı adı</param>
    Task SendEmailChangeVerificationAsync(string to, string verificationToken, string userName);
}
