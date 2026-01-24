namespace Application.DTOs.User;

/// <summary>
/// Kullanıcı özet bilgisi (Post/Thread listelerinde kullanılır)
/// </summary>
public class UserSummaryDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? ProfileImg { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
