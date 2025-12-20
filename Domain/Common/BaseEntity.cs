namespace Domain.Common;

public abstract class BaseEntity
{
   public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedDate { get; set; }
    public bool IsDeleted { get; set; } = false; // Soft delete flag
    public bool Recstatus { get; set; } = true; // true = aktif, false = pasif
}