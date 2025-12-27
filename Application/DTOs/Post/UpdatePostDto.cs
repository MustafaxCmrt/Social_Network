namespace Application.DTOs.Post;

public class UpdatePostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public string? Img { get; set; }
}
