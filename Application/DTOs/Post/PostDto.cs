using Application.DTOs.User;

namespace Application.DTOs.Post;

public class PostDto
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public string ThreadTitle { get; set; } = null!;
    public int UserId { get; set; }
    public string Content { get; set; } = null!;
    public string? Img { get; set; }
    public bool IsSolution { get; set; }
    public int UpvoteCount { get; set; }
    public int? ParentPostId { get; set; } // null = ana yorum, dolu = cevap
    public int ReplyCount { get; set; } // Bu yoruma kaç cevap gelmiş
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // İlişkili veri
    public UserSummaryDto? User { get; set; }
}
