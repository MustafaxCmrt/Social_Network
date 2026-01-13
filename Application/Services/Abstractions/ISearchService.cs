using Application.DTOs.Search;

namespace Application.Services.Abstractions;

/// <summary>
/// Arama işlemlerini yöneten interface
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Thread başlıklarında ve içeriğinde arama yapar
    /// </summary>
    /// <param name="query">Arama terimi</param>
    /// <param name="categoryId">Kategori filtresi (opsiyonel)</param>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Thread arama sonuçları</returns>
    Task<SearchResponseDto<SearchThreadResultDto>> SearchThreadsAsync(
        string query,
        int? categoryId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Post içeriğinde arama yapar
    /// </summary>
    /// <param name="query">Arama terimi</param>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Post arama sonuçları</returns>
    Task<SearchResponseDto<SearchPostResultDto>> SearchPostsAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı adı, ad ve soyadında arama yapar
    /// </summary>
    /// <param name="query">Arama terimi</param>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kullanıcı arama sonuçları</returns>
    Task<SearchResponseDto<SearchUserResultDto>> SearchUsersAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm kaynaklarda arama yapar (threads, posts, users)
    /// </summary>
    /// <param name="query">Arama terimi</param>
    /// <param name="limit">Her kategoriden maksimum sonuç sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Birleşik arama sonuçları</returns>
    Task<UnifiedSearchResultDto> SearchAllAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default);
}
