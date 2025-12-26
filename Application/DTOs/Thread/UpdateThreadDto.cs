namespace Application.DTOs.Thread;

public class UpdateThreadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool? IsSolved { get; set; }
    public int CategoryId { get; set; }
}
