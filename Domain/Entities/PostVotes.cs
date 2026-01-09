using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Post beğeni (upvote) tablosu
/// Bir kullanıcı bir post'a sadece 1 kere upvote verebilir
/// </summary>
public class PostVotes : BaseEntity
{
    public int PostId { get; set; } // Foreign Key - Zorunlu
    public int UserId { get; set; } // Foreign Key - Zorunlu

    // NAVIGATION PROPERTIES
    /// <summary>
    /// Hangi post beğenildi?
    /// </summary>
    public Posts Post { get; set; } = null!;
    
    /// <summary>
    /// Kim beğendi?
    /// </summary>
    public Users User { get; set; } = null!;
}
