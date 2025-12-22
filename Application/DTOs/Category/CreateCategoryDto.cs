namespace Application.DTOs.Category;

public class CreateCategoryDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}
