namespace Application.DTOs.Post;

public class PostDto
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = null!;
    public string? Img { get; set; }
    public bool IsSolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
