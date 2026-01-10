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
    
    // Alt Kategori Desteği
    public int? ParentCategoryId { get; set; } // Üst kategori ID'si (null ise ana kategori)
    public int SubCategoryCount { get; set; } // Bu kategorinin alt kategori sayısı
    
    // Audit trail - kim oluşturdu/güncelledi
    public int? CreatedUserId { get; set; }
    public int? UpdatedUserId { get; set; }
}
