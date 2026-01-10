namespace Application.DTOs.Post;

/// <summary>
/// Upvote işlemi response DTO
/// </summary>
public class UpvoteResponseDto
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
    
    /// <summary>
    /// İşlem mesajı
    /// </summary>
    public string Message { get; set; } = null!;
}
