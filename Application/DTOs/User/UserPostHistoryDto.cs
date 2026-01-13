using Application.DTOs.Post;

namespace Application.DTOs.User;

/// <summary>
/// Kullanıcının post geçmişi response DTO
/// </summary>
public class UserPostHistoryDto
{
    /// <summary>
    /// Kullanıcı ID'si
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Toplam post sayısı
    /// </summary>
    public int TotalPosts { get; set; }

    /// <summary>
    /// Mevcut sayfa numarası
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Sayfa başına öğe sayısı
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Post listesi
    /// </summary>
    public List<PostDto> Posts { get; set; } = new();
}
