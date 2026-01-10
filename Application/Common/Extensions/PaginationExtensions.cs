using Application.DTOs.Common;

namespace Application.Common.Extensions;

/// <summary>
/// Pagination için extension metodlar
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// IEnumerable koleksiyonunu sayfalandırır
    /// </summary>
    /// <typeparam name="T">DTO tipi</typeparam>
    /// <param name="source">Kaynak koleksiyon</param>
    /// <param name="page">Sayfa numarası (1-indexed)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <returns>Sayfalanmış sonuç</returns>
    public static PagedResultDto<T> ToPagedResult<T>(
        this IEnumerable<T> source,
        int page,
        int pageSize)
    {
        // Parametreleri normalize et
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : pageSize;

        // Koleksiyonu listeye çevir (multiple enumeration önlemek için)
        var items = source.ToList();
        
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var pagedItems = items
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new PagedResultDto<T>
        {
            Items = pagedItems,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// IEnumerable koleksiyonunu sayfalandırır ve DTO'ya map eder
    /// </summary>
    /// <typeparam name="TSource">Kaynak entity tipi</typeparam>
    /// <typeparam name="TDestination">Hedef DTO tipi</typeparam>
    /// <param name="source">Kaynak koleksiyon</param>
    /// <param name="page">Sayfa numarası (1-indexed)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="mapFunction">Entity'den DTO'ya mapping fonksiyonu</param>
    /// <returns>Sayfalanmış ve map edilmiş sonuç</returns>
    public static PagedResultDto<TDestination> ToPagedResult<TSource, TDestination>(
        this IEnumerable<TSource> source,
        int page,
        int pageSize,
        Func<TSource, TDestination> mapFunction)
    {
        // Parametreleri normalize et
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : pageSize;

        // Koleksiyonu listeye çevir
        var items = source.ToList();
        
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var pagedItems = items
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(mapFunction)
            .ToList();

        return new PagedResultDto<TDestination>
        {
            Items = pagedItems,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
