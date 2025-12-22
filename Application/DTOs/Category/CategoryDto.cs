namespace Application.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ThreadCount { get; set; } // Bu kategorideki konu sayısı
}
