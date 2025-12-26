namespace Application.DTOs.Thread;

public class CreateThreadDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int CategoryId { get; set; }
}
