using Application.DTOs.Search;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Arama işlemleri için API controller
/// </summary>
public class SearchController : AppController
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Thread başlıklarında ve içeriğinde arama yapar
    /// </summary>
    /// <param name="query">Arama terimi (zorunlu)</param>
    /// <param name="categoryId">Kategori filtresi (opsiyonel)</param>
    /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 10)</param>
    /// <returns>200 OK - Thread arama sonuçları</returns>
    [HttpGet("threads")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchResponseDto<SearchThreadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchThreads(
        [FromQuery] string query,
        [FromQuery] int? categoryId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Parametreleri doğrula
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama terimi boş olamaz" });
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        var results = await _searchService.SearchThreadsAsync(
            query,
            categoryId,
            pageNumber,
            pageSize,
            HttpContext.RequestAborted);

        return Ok(results);
    }

    /// <summary>
    /// Post içeriğinde arama yapar
    /// </summary>
    /// <param name="query">Arama terimi (zorunlu)</param>
    /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 10)</param>
    /// <returns>200 OK - Post arama sonuçları</returns>
    [HttpGet("posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchResponseDto<SearchPostResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Parametreleri doğrula
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama terimi boş olamaz" });
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        var results = await _searchService.SearchPostsAsync(
            query,
            pageNumber,
            pageSize,
            HttpContext.RequestAborted);

        return Ok(results);
    }

    /// <summary>
    /// Kullanıcı adı, ad ve soyadında arama yapar
    /// </summary>
    /// <param name="query">Arama terimi (zorunlu)</param>
    /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 10)</param>
    /// <returns>200 OK - Kullanıcı arama sonuçları</returns>
    [HttpGet("users")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchResponseDto<SearchUserResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Parametreleri doğrula
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama terimi boş olamaz" });
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        var results = await _searchService.SearchUsersAsync(
            query,
            pageNumber,
            pageSize,
            HttpContext.RequestAborted);

        return Ok(results);
    }

    /// <summary>
    /// Tüm kaynaklarda arama yapar (threads, posts, users)
    /// Her kategoriden sınırlı sayıda sonuç döner
    /// </summary>
    /// <param name="query">Arama terimi (zorunlu)</param>
    /// <param name="limit">Her kategoriden maksimum sonuç sayısı (varsayılan: 5)</param>
    /// <returns>200 OK - Birleşik arama sonuçları</returns>
    [HttpGet("all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UnifiedSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAll(
        [FromQuery] string query,
        [FromQuery] int limit = 5)
    {
        // Parametreleri doğrula
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama terimi boş olamaz" });
        }

        if (limit < 1) limit = 5;
        if (limit > 20) limit = 20; // Maksimum 20 öğe/kategori

        var results = await _searchService.SearchAllAsync(
            query,
            limit,
            HttpContext.RequestAborted);

        return Ok(results);
    }
}
