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

        var queryLower = query.ToLower();
        var catId = categoryId;

        var (threads, totalResults) = await _unitOfWork.Threads.FindPagedAsync(
            predicate: t => (t.Title.ToLower().Contains(queryLower) ||
                            t.Content.ToLower().Contains(queryLower)) &&
                           (!catId.HasValue || t.CategoryId == catId.Value),
            include: q => q.Include(t => t.User).Include(t => t.Category),
            orderBy: q => q.OrderByDescending(t => t.CreatedAt),
            page: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        var results = threads.Select(t => new SearchThreadResultDto
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
        }).ToList();

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

        var queryLower = query.ToLower();

        var (posts, totalResults) = await _unitOfWork.Posts.FindPagedAsync(
            predicate: p => p.Content.ToLower().Contains(queryLower),
            include: q => q.Include(p => p.User).Include(p => p.Thread),
            orderBy: q => q.OrderByDescending(p => p.CreatedAt),
            page: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        var results = posts.Select(p => new SearchPostResultDto
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
        }).ToList();

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

        var queryLower = query.ToLower();

        var (users, totalResults) = await _unitOfWork.Users.FindPagedAsync(
            predicate: u => u.Username.ToLower().Contains(queryLower) ||
                           u.FirstName.ToLower().Contains(queryLower) ||
                           u.LastName.ToLower().Contains(queryLower),
            orderBy: q => q.OrderBy(u => u.Username),
            page: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        var results = users.Select(u => new SearchUserResultDto
        {
            UserId = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            ProfileImg = u.ProfileImg,
            Role = u.Role.ToString(),
            TotalThreads = 0,
            TotalPosts = 0,
            CreatedAt = u.CreatedAt
        }).ToList();

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
