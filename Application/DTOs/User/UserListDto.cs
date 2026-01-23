namespace Application.DTOs.User;

/// <summary>
/// Kullanıcı listesi için özet bilgi DTO
/// </summary>
public class UserListDto
{
    /// <summary>
    /// Kullanıcının ID'si
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Profil resmi URL
    /// </summary>
    public string? ProfileImg { get; set; }

    /// <summary>
    /// Email adresi
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Kullanıcının rolü
    /// </summary>
    public string Role { get; set; } = null!;

    /// <summary>
    /// Hesap aktif mi?
    /// </summary>
    public bool IsActive { get; set; }
}
