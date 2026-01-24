namespace Application.DTOs.Category;

/// <summary>
/// Kategori özet bilgisi (Thread listelerinde kullanılır)
/// </summary>
public class CategorySummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
}
