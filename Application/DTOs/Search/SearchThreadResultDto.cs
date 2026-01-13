namespace Application.DTOs.Search;

/// <summary>
/// Thread arama sonucu DTO
/// </summary>
public class SearchThreadResultDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int ViewCount { get; set; }
    public bool IsSolved { get; set; }
    public int PostCount { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
