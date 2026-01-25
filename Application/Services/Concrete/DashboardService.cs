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
            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted, cancellationToken);
            
            var topUsersList = new List<TopUserDto>();

            foreach (var user in allUsers)
            {
                var threadCount = await _unitOfWork.Threads.CountAsync(t => t.UserId == user.Id && !t.IsDeleted, cancellationToken);
                var postCount = await _unitOfWork.Posts.CountAsync(p => p.UserId == user.Id && !p.IsDeleted, cancellationToken);

                topUsersList.Add(new TopUserDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    ThreadCount = threadCount,
                    PostCount = postCount,
                    TotalActivity = threadCount + postCount
                });
            }

            return topUsersList
                .OrderByDescending(x => x.TotalActivity)
                .Take(topCount)
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
            var allReports = await _unitOfWork.Reports.FindAsync(r => !r.IsDeleted, cancellationToken);

            var groupedReports = allReports
                .GroupBy(r => new { r.ReportedUserId, r.ReportedThreadId, r.ReportedPostId })
                .Select(g => new
                {
                    ContentId = g.Key.ReportedUserId ?? g.Key.ReportedThreadId ?? g.Key.ReportedPostId ?? 0,
                    ContentType = g.Key.ReportedUserId != null ? "User" :
                                  g.Key.ReportedThreadId != null ? "Thread" : "Post",
                    ReportCount = g.Count(),
                    LastReportedAt = g.Max(r => r.CreatedAt),
                    UserId = g.Key.ReportedUserId,
                    ThreadId = g.Key.ReportedThreadId,
                    PostId = g.Key.ReportedPostId
                })
                .OrderByDescending(x => x.ReportCount)
                .Take(topCount)
                .ToList();

            var result = new List<TopReportedContentDto>();

            foreach (var item in groupedReports)
            {
                string contentPreview = "Unknown";

                if (item.ContentType == "User" && item.UserId.HasValue)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(item.UserId.Value, cancellationToken);
                    contentPreview = user != null ? $"@{user.Username}" : "Deleted User";
                }
                else if (item.ContentType == "Thread" && item.ThreadId.HasValue)
                {
                    var thread = await _unitOfWork.Threads.GetByIdAsync(item.ThreadId.Value, cancellationToken);
                    contentPreview = thread != null ? thread.Title : "Deleted Thread";
                }
                else if (item.ContentType == "Post" && item.PostId.HasValue)
                {
                    var post = await _unitOfWork.Posts.GetByIdAsync(item.PostId.Value, cancellationToken);
                    contentPreview = post != null ? (post.Content.Length > 50 ? post.Content.Substring(0, 50) + "..." : post.Content) : "Deleted Post";
                }

                result.Add(new TopReportedContentDto
                {
                    ContentId = item.ContentId,
                    ContentType = item.ContentType,
                    ContentPreview = contentPreview,
                    ReportCount = item.ReportCount,
                    LastReportedAt = item.LastReportedAt
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top reported content");
            throw;
        }
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
        var now = DateTime.UtcNow;
        var allBans = await _unitOfWork.UserBans.FindAsync(b => !b.IsDeleted, cancellationToken);
        var activeBans = allBans.Where(b => b.ExpiresAt == null || b.ExpiresAt > now).Select(b => b.UserId).Distinct().Count();

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

        var allBans = await _unitOfWork.UserBans.FindAsync(b => !b.IsDeleted, cancellationToken);
        var allMutes = await _unitOfWork.UserMutes.FindAsync(m => !m.IsDeleted, cancellationToken);

        var activeBans = allBans.Count(b => b.ExpiresAt == null || b.ExpiresAt > now);
        var activeMutes = allMutes.Count(m => m.ExpiresAt > now);

        var totalBans = allBans.Count();
        var totalMutes = allMutes.Count();

        var bansToday = allBans.Count(b => b.CreatedAt >= today);
        var mutesToday = allMutes.Count(m => m.CreatedAt >= today);

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
