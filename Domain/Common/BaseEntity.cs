namespace Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    
    // Audit trail - Zaman damgaları
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedDate { get; set; }
    
    // Audit trail - Kullanıcı takibi
    public int? CreatedUserId { get; set; } // Kim oluşturdu
    public int? UpdatedUserId { get; set; } // Kim güncelledi
    public int? DeletedUserId { get; set; } // Kim sildi
    
    // Durum alanları
    public bool IsDeleted { get; set; } = false; // Soft delete flag
    public bool Recstatus { get; set; } = true; // true = aktif, false = pasif
}