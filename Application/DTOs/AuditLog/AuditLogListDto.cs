namespace Application.DTOs.AuditLog;

/// <summary>
/// Audit log liste DTO (özet bilgi)
/// </summary>
public record AuditLogListDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string Action { get; init; } = null!;
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public bool Success { get; init; }
    public DateTime CreatedAt { get; init; }
}
