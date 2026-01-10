namespace Application.DTOs.Post;

/// <summary>
/// Kullanıcının bir post'a verdiği oy durumu
/// </summary>
public class VoteStatusDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public int PostId { get; set; }
    
    /// <summary>
    /// Kullanıcı bu post'u beğenmiş mi?
    /// </summary>
    public bool IsUpvoted { get; set; }
    
    /// <summary>
    /// Post'un toplam upvote sayısı
    /// </summary>
    public int TotalUpvotes { get; set; }
}
