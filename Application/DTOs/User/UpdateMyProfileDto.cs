namespace Application.DTOs.User;

/// <summary>
/// Kullanıcının kendi profilini güncellemesi için DTO
/// </summary>
public class UpdateMyProfileDto
{
    /// <summary>
    /// Kullanıcının adı
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Kullanıcının soyadı
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Kullanıcı adı (değiştirilmek isteniyorsa)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Email adresi (değiştirilmek isteniyorsa)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Profil resmi URL'i
    /// </summary>
    public string? ProfileImg { get; set; }

    /// <summary>
    /// Yeni şifre (değiştirilmek isteniyorsa)
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// Yeni şifre tekrarı (doğrulama için)
    /// </summary>
    public string? NewPasswordConfirm { get; set; }

    /// <summary>
    /// Mevcut şifre (değişiklik için doğrulama)
    /// </summary>
    public string? CurrentPassword { get; set; }
}
