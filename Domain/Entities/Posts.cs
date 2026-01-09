using Domain.Common;

namespace Domain.Entities;

public class Posts : BaseEntity
{
    public string Content { get; set; } = null!; // Zorunlu - post içeriği olmalı
    public string? Img { get; set; } // Opsiyonel - post'ta resim olabilir
    public bool IsSolution { get; set; } = false; // Zorunlu - başlangıçta çözüm değil
    public int UpvoteCount { get; set; } = 0; // Zorunlu - başlangıçta 0 upvote

    public int ThreadId { get; set; } // Foreign Key - Zorunlu
    public int UserId { get; set; } // Foreign Key - Zorunlu
    public int? ParentPostId { get; set; } // Foreign Key - Opsiyonel (null = ana yorum, dolu = cevap)

    // NAVIGATION PROPERTIES
    // Bu cevap hangi konuya ait?
    public Threads Thread { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur
    
    // Bu cevabı kim yazdı?
    public Users User { get; set; } = null!; // Navigation property - EF Core tarafından doldurulur
    
    // Bu yorum başka bir yorumun cevabı mı? (üst yorum)
    public Posts? ParentPost { get; set; } // Navigation property - Opsiyonel
    
    // Bu yoruma gelen cevaplar (alt yorumlar)
    public ICollection<Posts> Replies { get; set; } = new List<Posts>();
    
    // Bu post'a gelen upvote'lar
    public ICollection<PostVotes> Votes { get; set; } = new List<PostVotes>();
    
    // Bu post'la ilgili bildirimler
    public ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();
}