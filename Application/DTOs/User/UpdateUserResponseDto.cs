namespace Application.DTOs.User;

/// <summary>
/// Kullanıcı güncelleme başarılı olduğunda dönen response DTO
/// </summary>
public class UpdateUserResponseDto
{
    /// <summary>
    /// Güncellenen kullanıcının ID'si
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
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = null!;

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

    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Başarı mesajı
    /// </summary>
    public string Message { get; set; } = "Profil başarıyla güncellendi";
}
