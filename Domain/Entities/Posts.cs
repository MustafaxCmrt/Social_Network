using Domain.Common;

namespace Domain.Entities;

public class Posts : BaseEntity
{
    public string Content { get; set; } = null!; // Zorunlu - post içeriği olmalı
    public string? Img { get; set; } // Opsiyonel - post'ta resim olabilir
    public bool IsSolution { get; set; } = false; // Zorunlu - başlangıçta çözüm değil

    public int ThreadId { get; set; } // Foreign Key - Zorunlu
    public int UserId { get; set; } // Foreign Key - Zorunlu

    // NAVIGATION PROPERTIES
    // Bu cevap hangi konuya ait?
    public Threads Thread { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur
    
    // Bu cevabı kim yazdı?
    public Users User { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur
}