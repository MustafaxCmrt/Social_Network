using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Denetim kaydı - Admin işlemlerini ve önemli sistem olaylarını takip eder
/// </summary>
public class AuditLogs : BaseEntity
{
    /// <summary>
    /// İşlemi yapan kullanıcının ID'si
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// İşlemi yapan kullanıcının adı (snapshot - user silinse bile bilgi kalır)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Yapılan işlem (örn: "BanUser", "DeletePost", "UpdateUser", "CreateCategory")
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Etkilenen entity tipi (örn: "User", "Post", "Thread", "Category")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Etkilenen entity'nin ID'si
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Eski değer (JSON formatında)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Yeni değer (JSON formatında)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// İşlemin yapıldığı IP adresi (güvenlik için 90 gün saklanır)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Kullanıcı cihaz bilgisi (User-Agent header)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// İşlem başarılı mı?
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Hata mesajı (başarısız işlemlerde)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Ek notlar
    /// </summary>
    public string? Notes { get; set; }

    // Navigation property
    public Users? User { get; set; }
}
