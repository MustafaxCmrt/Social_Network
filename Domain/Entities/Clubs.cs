using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Kulüpler
/// Üniversite öğrenci kulüpleri
/// </summary>
public class Clubs : BaseEntity
{
    /// <summary>
    /// Kulüp adı
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// URL-friendly isim (cs-kulubu, muzik-kulubu vs.)
    /// </summary>
    public string Slug { get; set; } = null!;
    
    /// <summary>
    /// Kulüp açıklaması
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Kulüp logosu URL
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// Kulüp kapak görseli URL (banner)
    /// </summary>
    public string? BannerUrl { get; set; }
    
    /// <summary>
    /// Herkese açık mı? (false ise sadece üyeler içeriği görebilir)
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// Üyelik için onay gerekli mi?
    /// </summary>
    public bool RequiresApproval { get; set; } = false;
    
    /// <summary>
    /// Üye sayısı (denormalized - performans için)
    /// </summary>
    public int MemberCount { get; set; } = 0;
    
    /// <summary>
    /// Kulübün kurucusu (başkan olarak atanır) (FK)
    /// </summary>
    public int FounderId { get; set; }
    
    // ==================== NAVIGATION PROPERTIES ====================
    
    /// <summary>
    /// Kulüp kurucusu
    /// </summary>
    public Users Founder { get; set; } = null!;
    
    /// <summary>
    /// Kulüp üyelikleri
    /// </summary>
    public ICollection<ClubMemberships> Memberships { get; set; } = new List<ClubMemberships>();
    
    /// <summary>
    /// Kulübe ait thread'ler
    /// </summary>
    public ICollection<Threads> Threads { get; set; } = new List<Threads>();
}
