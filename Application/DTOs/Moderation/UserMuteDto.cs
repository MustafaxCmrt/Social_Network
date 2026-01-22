namespace Application.DTOs.Moderation;

/// <summary>
/// Mute detaylarını göstermek için kullanılan DTO
/// </summary>
public record UserMuteDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int MutedByUserId { get; init; }
    public string MutedByUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime MutedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; }
}
