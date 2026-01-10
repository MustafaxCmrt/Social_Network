namespace Application.DTOs.Post;

public class CreatePostDto
{
    public int ThreadId { get; set; }
    public string Content { get; set; } = null!;
    public string? Img { get; set; }
    
    /// <summary>
    /// Cevap verilecek yorumun ID'si (null = ana yorum, dolu = cevap)
    /// </summary>
    public int? ParentPostId { get; set; }
}
