using Domain.Common;

namespace Domain.Entities;

public class Categories : BaseEntity
{
    public string Title { get; set; } = null!; // Zorunlu - kategorinin başlığı olmalı
    public string Slug { get; set; } = null!; // Zorunlu - URL için slug olmalı
    public string? Description { get; set; } // Opsiyonel - açıklama olmayabilir
    
    // Alt Kategori Desteği
    public int? ParentCategoryId { get; set; } // Nullable - ana kategoriler için null
    
    /// <summary>
    /// Kulüp ID - Nullable (null ise normal forum kategorisi, değilse kulüp kategorisi)
    /// </summary>
    public int? ClubId { get; set; } // Foreign Key - Opsiyonel
    
    // Navigation Properties
    public Categories? ParentCategory { get; set; } // Üst kategori
    public ICollection<Categories> SubCategories { get; set; } = new List<Categories>(); // Alt kategoriler
    public ICollection<Threads> Threads { get; set; } = new List<Threads>();
    
    /// <summary>
    /// Kategorinin ait olduğu kulüp (opsiyonel)
    /// </summary>
    public Clubs? Club { get; set; } // Navigation property - Kulüp kategorisi ise doldurulur
}