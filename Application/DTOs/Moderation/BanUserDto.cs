namespace Application.DTOs.Moderation;

/// <summary>
/// Kullanıcıyı yasaklamak için kullanılan DTO
/// </summary>
public record BanUserDto
{
    /// <summary>
    /// Yasaklanacak kullanıcının ID'si
    /// </summary>
    public int UserId { get; init; }
    
    /// <summary>
    /// Yasaklama sebebi (zorunlu)
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// Yasak bitiş tarihi (null = kalıcı ban)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
