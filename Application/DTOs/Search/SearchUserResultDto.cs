namespace Application.DTOs.Search;

/// <summary>
/// Kullanıcı arama sonucu DTO
/// </summary>
public class SearchUserResultDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? ProfileImg { get; set; }
    public string Role { get; set; } = null!;
    public int TotalThreads { get; set; }
    public int TotalPosts { get; set; }
    public DateTime CreatedAt { get; set; }
}
