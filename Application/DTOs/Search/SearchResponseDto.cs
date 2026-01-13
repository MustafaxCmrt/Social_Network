namespace Application.DTOs.Search;

/// <summary>
/// Arama sonuçları response DTO (sayfalama ile)
/// </summary>
public class SearchResponseDto<T>
{
    public List<T> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string Query { get; set; } = null!;
}
