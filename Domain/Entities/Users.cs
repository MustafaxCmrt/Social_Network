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
    
    // Refresh Token versiyonlama - her login/logout/refresh'de artar
    // Token içinde bu versiyon claim olarak taşınır, veritabanı ile eşleşmezse token geçersiz olur
    public int RefreshTokenVersion { get; set; } = 0;
    
    // Bir kullanıcının birden fazla konusu olabilir:
    public ICollection<Threads> Threads { get; set; } = new List<Threads>();
    
    // Bir kullanıcının birden fazla mesajı (cevabı) olabilir:
    public ICollection<Posts> Posts { get; set; } = new List<Posts>();
}