namespace Application.DTOs.Search;

/// <summary>
/// Post arama sonucu DTO
/// </summary>
public class SearchPostResultDto
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public string? Img { get; set; }
    public bool IsSolution { get; set; }
    public int UpvoteCount { get; set; }
    public int ThreadId { get; set; }
    public string ThreadTitle { get; set; } = null!;
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public int? ParentPostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
