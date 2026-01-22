using Domain.Common;

namespace Domain.Entities;

public class Threads : BaseEntity
{
    public string Title { get; set; } = null!; // Zorunlu - konu başlığı olmalı
    public string Content { get; set; } = null!; // Zorunlu - içerik olmalı
    public int ViewCount { get; set; } = 0; // Zorunlu - başlangıçta 0
    public bool IsSolved { get; set; } = false; // Zorunlu - başlangıçta çözülmemiş
    public bool IsLocked { get; set; } = false; // Zorunlu - başlangıçta kilitli değil (admin thread'i kilitleyebilir)
    public int PostCount { get; set; } = 0; // Zorunlu - başlangıçta 0 yorum
    
    public int UserId { get; set; } // Foreign Key - Zorunlu
    public int CategoryId { get; set; } // Foreign Key - Zorunlu

    // NAVIGATION PROPERTIES (Nesne olarak erişim)
    // Kod yazarken thread.User.Email diyebilmen için:
    public Users User { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur
    
    // Kod yazarken thread.Category.Title diyebilmen için:
    public Categories Category { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur

    // Bir konunun altında birden fazla cevap (Post) olabilir:
    public ICollection<Posts> Posts { get; set; } = new List<Posts>();
    
    // Bu thread ile ilgili bildirimler
    public ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();
}