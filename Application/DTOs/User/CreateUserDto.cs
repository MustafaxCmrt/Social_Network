namespace Application.DTOs.User;

/// <summary>
/// Yeni kullanıcı oluşturma request DTO
/// Admin tarafından kullanılır ve rol belirlenebilir
/// </summary>
public class CreateUserDto
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
    /// Kullanıcı adı (unique)
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Email adresi (unique)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Şifre (minimum 6 karakter)
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Kullanıcının rolü (User, Moderator, Admin)
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// Hesap aktif mi? (varsayılan: true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
