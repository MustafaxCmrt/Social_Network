namespace Application.DTOs.AuditLog;

/// <summary>
/// Yeni audit log kaydı oluşturmak için DTO
/// </summary>
public record CreateAuditLogDto
{
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string Action { get; init; } = null!;
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public string? Notes { get; init; }
}
