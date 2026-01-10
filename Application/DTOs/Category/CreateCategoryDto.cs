namespace Application.DTOs.Category;

public class CreateCategoryDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; } // Alt kategori oluşturmak için üst kategori ID'si
}
