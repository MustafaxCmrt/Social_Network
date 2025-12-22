namespace Application.DTOs.Category;

public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}
