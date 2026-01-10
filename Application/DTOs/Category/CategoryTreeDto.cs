namespace Application.DTOs.Category;

/// <summary>
/// Hiyerarşik kategori tree yapısı için DTO
/// </summary>
public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int ThreadCount { get; set; }
    public int? ParentCategoryId { get; set; }
    
    /// <summary>
    /// Bu kategorinin alt kategorileri (recursive)
    /// </summary>
    public List<CategoryTreeDto> SubCategories { get; set; } = new();
}
