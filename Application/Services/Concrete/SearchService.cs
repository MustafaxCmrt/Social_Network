using Application.DTOs.Search;
using Application.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Arama işlemlerini gerçekleştiren servis
/// </summary>
public class SearchService : ISearchService
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Thread başlıklarında ve içeriğinde arama yapar
    /// </summary>
    public async Task<SearchResponseDto<SearchThreadResultDto>> SearchThreadsAsync(
        string query,
        int? categoryId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponseDto<SearchThreadResultDto>
            {
                Results = new List<SearchThreadResultDto>(),
                TotalResults = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                Query = query ?? string.Empty
            };
        }

        // Tüm thread'leri User ve Category ile birlikte al
        var allThreads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: query => query
                .Include(t => t.User)
                .Include(t => t.Category),
            cancellationToken: cancellationToken);

        // Arama ve filtreleme
        var queryLower = query.ToLower();
        var filteredThreads = allThreads
            .Where(t => !t.IsDeleted &&
                       (t.Title.ToLower().Contains(queryLower) ||
                        t.Content.ToLower().Contains(queryLower)))
            .AsEnumerable();

        // Kategori filtresi varsa uygula
        if (categoryId.HasValue)
        {
            filteredThreads = filteredThreads.Where(t => t.CategoryId == categoryId.Value);
        }

        var totalResults = filteredThreads.Count();
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        // Sayfalama ve DTO'ya çevir
        var results = filteredThreads
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SearchThreadResultDto
            {
                Id = t.Id,
                Title = t.Title,
                Content = t.Content.Length > 200 ? t.Content.Substring(0, 200) + "..." : t.Content,
                ViewCount = t.ViewCount,
                IsSolved = t.IsSolved,
                PostCount = t.PostCount,
                UserId = t.UserId,
                Username = t.User?.Username ?? "Unknown",
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Title ?? "Unknown",
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToList();

        return new SearchResponseDto<SearchThreadResultDto>
        {
            Results = results,
            TotalResults = totalResults,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Query = query
        };
    }

    /// <summary>
    /// Post içeriğinde arama yapar
    /// </summary>
    public async Task<SearchResponseDto<SearchPostResultDto>> SearchPostsAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponseDto<SearchPostResultDto>
            {
                Results = new List<SearchPostResultDto>(),
                TotalResults = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                Query = query ?? string.Empty
            };
        }

        // Tüm post'ları User ve Thread ile birlikte al
        var allPosts = await _unitOfWork.Posts.GetAllWithIncludesAsync(
            include: query => query
                .Include(p => p.User)
                .Include(p => p.Thread),
            cancellationToken: cancellationToken);

        // Arama
        var queryLower = query.ToLower();
        var filteredPosts = allPosts
            .Where(p => !p.IsDeleted && p.Content.ToLower().Contains(queryLower))
            .ToList();

        var totalResults = filteredPosts.Count;
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        // Sayfalama ve DTO'ya çevir
        var results = filteredPosts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SearchPostResultDto
            {
                Id = p.Id,
                Content = p.Content.Length > 200 ? p.Content.Substring(0, 200) + "..." : p.Content,
                Img = p.Img,
                IsSolution = p.IsSolution,
                UpvoteCount = p.UpvoteCount,
                ThreadId = p.ThreadId,
                ThreadTitle = p.Thread?.Title ?? "Unknown",
                UserId = p.UserId,
                Username = p.User?.Username ?? "Unknown",
                ParentPostId = p.ParentPostId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToList();

        return new SearchResponseDto<SearchPostResultDto>
        {
            Results = results,
            TotalResults = totalResults,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Query = query
        };
    }

    /// <summary>
    /// Kullanıcı adı, ad ve soyadında arama yapar
    /// </summary>
    public async Task<SearchResponseDto<SearchUserResultDto>> SearchUsersAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponseDto<SearchUserResultDto>
            {
                Results = new List<SearchUserResultDto>(),
                TotalResults = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                Query = query ?? string.Empty
            };
        }

        // Tüm kullanıcıları al
        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);

        // Arama
        var queryLower = query.ToLower();
        var filteredUsers = allUsers
            .Where(u => !u.IsDeleted &&
                       (u.Username.ToLower().Contains(queryLower) ||
                        u.FirstName.ToLower().Contains(queryLower) ||
                        u.LastName.ToLower().Contains(queryLower)))
            .ToList();

        var totalResults = filteredUsers.Count;
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        // Sayfalama ve DTO'ya çevir (Thread ve Post sayılarıyla birlikte)
        var results = new List<SearchUserResultDto>();

        var paginatedUsers = filteredUsers
            .OrderBy(u => u.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var user in paginatedUsers)
        {
            var totalThreads = await _unitOfWork.Threads.CountAsync(
                t => t.UserId == user.Id && !t.IsDeleted,
                cancellationToken
            );

            var totalPosts = await _unitOfWork.Posts.CountAsync(
                p => p.UserId == user.Id && !p.IsDeleted,
                cancellationToken
            );

            results.Add(new SearchUserResultDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                ProfileImg = user.ProfileImg,
                Role = user.Role.ToString(),
                TotalThreads = totalThreads,
                TotalPosts = totalPosts,
                CreatedAt = user.CreatedAt
            });
        }

        return new SearchResponseDto<SearchUserResultDto>
        {
            Results = results,
            TotalResults = totalResults,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Query = query
        };
    }

    /// <summary>
    /// Tüm kaynaklarda arama yapar (threads, posts, users)
    /// </summary>
    public async Task<UnifiedSearchResultDto> SearchAllAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new UnifiedSearchResultDto
            {
                Query = query ?? string.Empty,
                Threads = new List<SearchThreadResultDto>(),
                Posts = new List<SearchPostResultDto>(),
                Users = new List<SearchUserResultDto>(),
                TotalThreads = 0,
                TotalPosts = 0,
                TotalUsers = 0
            };
        }

        // Thread'lerde ara
        var threadResults = await SearchThreadsAsync(query, null, 1, limit, cancellationToken);

        // Post'larda ara
        var postResults = await SearchPostsAsync(query, 1, limit, cancellationToken);

        // Kullanıcılarda ara
        var userResults = await SearchUsersAsync(query, 1, limit, cancellationToken);

        return new UnifiedSearchResultDto
        {
            Query = query,
            Threads = threadResults.Results,
            Posts = postResults.Results,
            Users = userResults.Results,
            TotalThreads = threadResults.TotalResults,
            TotalPosts = postResults.TotalResults,
            TotalUsers = userResults.TotalResults
        };
    }
}
