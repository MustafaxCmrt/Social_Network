namespace Application.DTOs.Search;

/// <summary>
/// Birleşik arama sonuçları (tüm arama sonuçlarını içerir)
/// </summary>
public class UnifiedSearchResultDto
{
    public string Query { get; set; } = null!;
    public List<SearchThreadResultDto> Threads { get; set; } = new();
    public List<SearchPostResultDto> Posts { get; set; } = new();
    public List<SearchUserResultDto> Users { get; set; } = new();
    public int TotalThreads { get; set; }
    public int TotalPosts { get; set; }
    public int TotalUsers { get; set; }
}
