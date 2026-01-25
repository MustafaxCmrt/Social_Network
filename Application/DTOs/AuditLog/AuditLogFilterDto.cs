namespace Application.DTOs.AuditLog;

/// <summary>
/// Audit log filtreleme parametreleri
/// </summary>
public record AuditLogFilterDto
{
    public int? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public bool? Success { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
