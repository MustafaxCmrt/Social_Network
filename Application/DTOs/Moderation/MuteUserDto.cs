namespace Application.DTOs.Moderation;

/// <summary>
/// Kullanıcıyı susturmak için kullanılan DTO
/// </summary>
public record MuteUserDto
{
    /// <summary>
    /// Susturulacak kullanıcının ID'si
    /// </summary>
    public int UserId { get; init; }
    
    /// <summary>
    /// Susturma sebebi (zorunlu)
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// Susturma bitiş tarihi (zorunlu - mute her zaman geçici)
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}
