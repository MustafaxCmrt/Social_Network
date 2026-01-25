using Application.DTOs.User;
using Application.DTOs.Category;

namespace Application.DTOs.Thread;

public class ThreadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int ViewCount { get; set; }
    public int PostCount { get; set; }
    public bool IsSolved { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // İlişkili veri
    public UserSummaryDto? User { get; set; }
    public CategorySummaryDto? Category { get; set; }
}
