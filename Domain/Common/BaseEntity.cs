namespace Domain.Common;

public class BaseEntity
{
    public int Id { get; set; } // Primary Key - EF Core tarafından otomatik atanır
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Zorunlu - oluşturulma zamanı
}