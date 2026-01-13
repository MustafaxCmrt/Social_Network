using Application.DTOs.Thread;

namespace Application.DTOs.User;

/// <summary>
/// Kullanıcının thread geçmişi response DTO
/// </summary>
public class UserThreadHistoryDto
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
    /// Toplam thread sayısı
    /// </summary>
    public int TotalThreads { get; set; }

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
    /// Thread listesi
    /// </summary>
    public List<ThreadDto> Threads { get; set; } = new();
}
