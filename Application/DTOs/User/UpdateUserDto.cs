namespace Application.DTOs.User;

/// <summary>
/// Kullanıcı güncelleme request DTO
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Güncellenecek kullanıcının ID'si
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Kullanıcının adı
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Kullanıcının soyadı
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Kullanıcı adı (opsiyonel - değiştirilebilir)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Email adresi (opsiyonel - değiştirilebilir)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Yeni şifre (opsiyonel - boşsa değiştirilmez)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Hesap aktif mi? (sadece admin değiştirebilir)
    /// </summary>
    public bool? IsActive { get; set; }
}
