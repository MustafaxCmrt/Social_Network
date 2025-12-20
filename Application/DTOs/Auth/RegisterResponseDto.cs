namespace Application.DTOs.Auth;

/// <summary>
/// Kullanıcı kayıt başarılı olduğunda dönen response DTO
/// </summary>
public record RegisterResponseDto
{
    /// <summary>
    /// Oluşturulan kullanıcının ID'si
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
    /// Başarı mesajı
    /// </summary>
    public string Message { get; set; } = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
}
