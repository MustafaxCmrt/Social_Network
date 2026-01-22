namespace Application.DTOs.Moderation;

/// <summary>
/// Ban detaylarını göstermek için kullanılan DTO
/// </summary>
public record UserBanDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int BannedByUserId { get; init; }
    public string BannedByUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime BannedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsPermanent => ExpiresAt == null;
}
