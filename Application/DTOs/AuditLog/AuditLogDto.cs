namespace Application.DTOs.AuditLog;

/// <summary>
/// Audit log detay DTO
/// </summary>
public record AuditLogDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string Action { get; init; } = null!;
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
