namespace Application.DTOs.Thread;

public class CreateThreadDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int CategoryId { get; set; }
    
    /// <summary>
    /// Kulüp ID - Nullable (null ise normal forum thread'i, değilse kulüp thread'i)
    /// </summary>
    public int? ClubId { get; set; }
}
