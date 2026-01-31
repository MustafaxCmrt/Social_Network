using Application.DTOs.Dashboard;
using Application.Services.Abstractions;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Dashboard istatistikleri servisi
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddMonths(-1);

            // Kullanıcı istatistikleri
            var userStats = await GetUserStatsAsync(today, weekAgo, monthAgo, cancellationToken);

            // İçerik istatistikleri
            var contentStats = await GetContentStatsAsync(today, weekAgo, cancellationToken);

            // Rapor istatistikleri
            var reportStats = await GetReportStatsAsync(today, weekAgo, cancellationToken);

            // Moderasyon istatistikleri
            var moderationStats = await GetModerationStatsAsync(today, cancellationToken);

            // Son 7 günlük aktivite
            var last7DaysActivity = await GetLast7DaysActivityAsync(weekAgo, cancellationToken);

            return new DashboardStatsDto
            {
                UserStats = userStats,
                ContentStats = contentStats,
                ReportStats = reportStats,
                ModerationStats = moderationStats,
                Last7DaysActivity = last7DaysActivity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            throw;
        }
    }

    public async Task<List<TopUserDto>> GetTopUsersAsync(int topCount = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _unitOfWork.DashboardQueries.GetTopUsersRowsAsync(topCount, cancellationToken);

            return rows
                .Select(x => new TopUserDto
                {
                    UserId = x.UserId,
                    Username = x.Username,
                    Email = x.Email,
                    ThreadCount = x.ThreadCount,
                    PostCount = x.PostCount,
                    TotalActivity = x.TotalActivity
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top users");
            throw;
        }
    }

    public async Task<List<TopReportedContentDto>> GetTopReportedContentAsync(int topCount = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _unitOfWork.DashboardQueries.GetTopReportedContentRowsAsync(topCount, cancellationToken);

            var userIds = rows
                .Where(r => r.ContentType == "User")
                .Select(r => r.ContentId)
                .Distinct()
                .ToList();

            var threadIds = rows
                .Where(r => r.ContentType == "Thread")
                .Select(r => r.ContentId)
                .Distinct()
                .ToList();

            var postIds = rows
                .Where(r => r.ContentType == "Post")
                .Select(r => r.ContentId)
                .Distinct()
                .ToList();

            var users = userIds.Count == 0
                ? []
                : await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);

            var threads = threadIds.Count == 0
                ? []
                : await _unitOfWork.Threads.FindAsync(t => threadIds.Contains(t.Id), cancellationToken);

            var posts = postIds.Count == 0
                ? []
                : await _unitOfWork.Posts.FindAsync(p => postIds.Contains(p.Id), cancellationToken);

            var userMap = users.ToDictionary(u => u.Id, u => u.Username);
            var threadMap = threads.ToDictionary(t => t.Id, t => t.Title);
            var postMap = posts.ToDictionary(p => p.Id, p => p.Content);

            return rows
                .Select(r => new TopReportedContentDto
                {
                    ContentId = r.ContentId,
                    ContentType = r.ContentType,
                    ContentPreview = BuildContentPreview(r.ContentType, r.ContentId, userMap, threadMap, postMap),
                    ReportCount = r.ReportCount,
                    LastReportedAt = r.LastReportedAt
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top reported content");
            throw;
        }
    }

    private static string BuildContentPreview(
        string contentType,
        int contentId,
        IReadOnlyDictionary<int, string> userMap,
        IReadOnlyDictionary<int, string> threadMap,
        IReadOnlyDictionary<int, string> postMap)
    {
        return contentType switch
        {
            "User" => userMap.TryGetValue(contentId, out var username)
                ? $"@{username}"
                : "Deleted User",
            "Thread" => threadMap.TryGetValue(contentId, out var title)
                ? title
                : "Deleted Thread",
            "Post" => postMap.TryGetValue(contentId, out var content)
                ? (content.Length > 50 ? content.Substring(0, 50) + "..." : content)
                : "Deleted Post",
            _ => "Unknown"
        };
    }

    #region Private Helper Methods

    private async Task<UserStatsDto> GetUserStatsAsync(DateTime today, DateTime weekAgo, DateTime monthAgo, CancellationToken cancellationToken)
    {
        var totalUsers = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        
        // Aktif kullanıcılar: Son 24 saatte giriş yapanlar
        var yesterday = today.AddDays(-1);
        var activeUsers = await _unitOfWork.Users.CountAsync(
            u => !u.IsDeleted && u.LastLoginAt.HasValue && u.LastLoginAt >= yesterday, 
            cancellationToken);
        
        var newUsersToday = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= today, cancellationToken);
        var newUsersThisWeek = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= weekAgo, cancellationToken);
        var newUsersThisMonth = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= monthAgo, cancellationToken);
        var totalAdmins = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted && u.Role == Roles.Admin, cancellationToken);

        // Aktif banlar
        // Not: Bir kullanıcı için birden fazla aktif ban kaydı varsa, bu count kullanıcı sayısından fazla olabilir.
        // Normal akışta aynı kullanıcıya ikinci aktif ban verilmiyor, bu yüzden pratikte doğru sayım olur.
        var now = DateTime.UtcNow;
        var activeBans = await _unitOfWork.UserBans.CountAsync(
            b => !b.IsDeleted && b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now),
            cancellationToken);

        return new UserStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            NewUsersToday = newUsersToday,
            NewUsersThisWeek = newUsersThisWeek,
            NewUsersThisMonth = newUsersThisMonth,
            TotalAdmins = totalAdmins,
            BannedUsers = activeBans
        };
    }

    private async Task<ContentStatsDto> GetContentStatsAsync(DateTime today, DateTime weekAgo, CancellationToken cancellationToken)
    {
        var totalThreads = await _unitOfWork.Threads.CountAsync(t => !t.IsDeleted, cancellationToken);
        var totalPosts = await _unitOfWork.Posts.CountAsync(p => !p.IsDeleted, cancellationToken);
        var threadsToday = await _unitOfWork.Threads.CountAsync(t => !t.IsDeleted && t.CreatedAt >= today, cancellationToken);
        var postsToday = await _unitOfWork.Posts.CountAsync(p => !p.IsDeleted && p.CreatedAt >= today, cancellationToken);
        var threadsThisWeek = await _unitOfWork.Threads.CountAsync(t => !t.IsDeleted && t.CreatedAt >= weekAgo, cancellationToken);
        var postsThisWeek = await _unitOfWork.Posts.CountAsync(p => !p.IsDeleted && p.CreatedAt >= weekAgo, cancellationToken);
        var lockedThreads = await _unitOfWork.Threads.CountAsync(t => !t.IsDeleted && t.IsLocked, cancellationToken);
        var totalCategories = await _unitOfWork.Categories.CountAsync(c => !c.IsDeleted, cancellationToken);

        return new ContentStatsDto
        {
            TotalThreads = totalThreads,
            TotalPosts = totalPosts,
            ThreadsToday = threadsToday,
            PostsToday = postsToday,
            ThreadsThisWeek = threadsThisWeek,
            PostsThisWeek = postsThisWeek,
            LockedThreads = lockedThreads,
            TotalCategories = totalCategories
        };
    }

    private async Task<ReportStatsDto> GetReportStatsAsync(DateTime today, DateTime weekAgo, CancellationToken cancellationToken)
    {
        var totalReports = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted, cancellationToken);
        var pendingReports = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.Status == ReportStatus.Pending, cancellationToken);
        var reviewedReports = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.Status == ReportStatus.Reviewed, cancellationToken);
        var resolvedReports = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.Status == ReportStatus.Resolved, cancellationToken);
        var rejectedReports = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.Status == ReportStatus.Rejected, cancellationToken);
        var reportsToday = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.CreatedAt >= today, cancellationToken);
        var reportsThisWeek = await _unitOfWork.Reports.CountAsync(r => !r.IsDeleted && r.CreatedAt >= weekAgo, cancellationToken);

        return new ReportStatsDto
        {
            TotalReports = totalReports,
            PendingReports = pendingReports,
            ReviewedReports = reviewedReports,
            ResolvedReports = resolvedReports,
            RejectedReports = rejectedReports,
            ReportsToday = reportsToday,
            ReportsThisWeek = reportsThisWeek
        };
    }

    private async Task<ModerationStatsDto> GetModerationStatsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activeBans = await _unitOfWork.UserBans.CountAsync(
            b => !b.IsDeleted && b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now),
            cancellationToken);

        var activeMutes = await _unitOfWork.UserMutes.CountAsync(
            m => !m.IsDeleted && m.IsActive && m.ExpiresAt > now,
            cancellationToken);

        var totalBans = await _unitOfWork.UserBans.CountAsync(b => !b.IsDeleted, cancellationToken);
        var totalMutes = await _unitOfWork.UserMutes.CountAsync(m => !m.IsDeleted, cancellationToken);

        var bansToday = await _unitOfWork.UserBans.CountAsync(b => !b.IsDeleted && b.CreatedAt >= today, cancellationToken);
        var mutesToday = await _unitOfWork.UserMutes.CountAsync(m => !m.IsDeleted && m.CreatedAt >= today, cancellationToken);

        return new ModerationStatsDto
        {
            ActiveBans = activeBans,
            ActiveMutes = activeMutes,
            TotalBans = totalBans,
            TotalMutes = totalMutes,
            BansToday = bansToday,
            MutesToday = mutesToday
        };
    }

    private async Task<List<DailyActivityDto>> GetLast7DaysActivityAsync(DateTime weekAgo, CancellationToken cancellationToken)
    {
        var activities = new List<DailyActivityDto>();

        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var nextDate = date.AddDays(1);

            var newUsers = await _unitOfWork.Users.CountAsync(
                u => !u.IsDeleted && u.CreatedAt >= date && u.CreatedAt < nextDate, cancellationToken);

            var newThreads = await _unitOfWork.Threads.CountAsync(
                t => !t.IsDeleted && t.CreatedAt >= date && t.CreatedAt < nextDate, cancellationToken);

            var newPosts = await _unitOfWork.Posts.CountAsync(
                p => !p.IsDeleted && p.CreatedAt >= date && p.CreatedAt < nextDate, cancellationToken);

            var newReports = await _unitOfWork.Reports.CountAsync(
                r => !r.IsDeleted && r.CreatedAt >= date && r.CreatedAt < nextDate, cancellationToken);

            activities.Add(new DailyActivityDto
            {
                Date = date,
                NewUsers = newUsers,
                NewThreads = newThreads,
                NewPosts = newPosts,
                NewReports = newReports
            });
        }

        return activities;
    }

    #endregion
}
