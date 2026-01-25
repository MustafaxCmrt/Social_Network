using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Users : BaseEntity
{
    public string FirstName { get; set; } = null!; // Zorunlu - kullanıcının adı
    public string LastName { get; set; } = null!; // Zorunlu - kullanıcının soyadı
    public string Username { get; set; } = null!; // Zorunlu - kullanıcı adı
    public string Email { get; set; } = null!; // Zorunlu - her kullanıcının emaili olmalı
    public string PasswordHash { get; set; } = null!; // Zorunlu - şifre hash'i olmalı
    public string? ProfileImg { get; set; } // Opsiyonel - kullanıcı profil resmi olmayabilir
    public Roles Role { get; set; } // Zorunlu - enum default değer alır (0)
    public bool IsActive { get; set; } = true; // Zorunlu - yeni kullanıcı aktif olarak başlar
    
    /// <summary>
    /// Email doğrulandı mı? (Kayıt sonrası email doğrulama)
    /// </summary>
    public bool EmailVerified { get; set; } = false; // Varsayılan: doğrulanmamış
    
    /// <summary>
    /// Email doğrulama token'ı (SHA256 hash)
    /// Doğrulandıktan sonra null olur
    /// </summary>
    public string? EmailVerificationToken { get; set; }
    
    /// <summary>
    /// Email doğrulama token'ının oluşturulma zamanı
    /// </summary>
    public DateTime? EmailVerificationTokenCreatedAt { get; set; }
    
    // Refresh Token versiyonlama - her login/logout/refresh'de artar
    // Token içinde bu versiyon claim olarak taşınır, veritabanı ile eşleşmezse token geçersiz olur
    public int RefreshTokenVersion { get; set; } = 0;
    
    /// <summary>
    /// Son giriş zamanı - Login olduğunda güncellenir
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    // Bir kullanıcının birden fazla konusu olabilir:
    public ICollection<Threads> Threads { get; set; } = new List<Threads>();
    
    // Bir kullanıcının birden fazla mesajı (cevabı) olabilir:
    public ICollection<Posts> Posts { get; set; } = new List<Posts>();
    
    // Bir kullanıcının verdiği upvote'lar:
    public ICollection<PostVotes> PostVotes { get; set; } = new List<PostVotes>();
    
    // Kullanıcıya gelen bildirimler:
    public ICollection<Notifications> ReceivedNotifications { get; set; } = new List<Notifications>();
    
    // Kullanıcının tetiklediği bildirimler (başkasına yorum yapınca o bildirim bu kullanıcı actor olur):
    public ICollection<Notifications> TriggeredNotifications { get; set; } = new List<Notifications>();
}