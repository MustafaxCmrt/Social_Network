namespace Application.DTOs.User;

/// <summary>
/// Public kullanıcı profil bilgileri
/// </summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfileImg { get; set; }
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Kullanıcının oluşturduğu toplam konu sayısı
    /// </summary>
    public int TotalThreads { get; set; }
    
    /// <summary>
    /// Kullanıcının yazdığı toplam yorum sayısı
    /// </summary>
    public int TotalPosts { get; set; }
}
