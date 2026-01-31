using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class DashboardQueryRepository : IDashboardQueryRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardQueryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopReportedContentRow>> GetTopReportedContentRowsAsync(
        int topCount,
        CancellationToken cancellationToken = default)
    {
        if (topCount < 1) topCount = 10;
        if (topCount > 100) topCount = 100;

        // Query filters (soft delete) DbContext seviyesinde zaten uygulanıyor.
        var userReports = _context.Reports
            .Where(r => r.ReportedUserId.HasValue)
            .GroupBy(r => r.ReportedUserId!.Value)
            .Select(g => new TopReportedContentRow(
                "User",
                g.Key,
                g.Count(),
                g.Max(r => r.CreatedAt)));

        var threadReports = _context.Reports
            .Where(r => r.ReportedThreadId.HasValue)
            .GroupBy(r => r.ReportedThreadId!.Value)
            .Select(g => new TopReportedContentRow(
                "Thread",
                g.Key,
                g.Count(),
                g.Max(r => r.CreatedAt)));

        var postReports = _context.Reports
            .Where(r => r.ReportedPostId.HasValue)
            .GroupBy(r => r.ReportedPostId!.Value)
            .Select(g => new TopReportedContentRow(
                "Post",
                g.Key,
                g.Count(),
                g.Max(r => r.CreatedAt)));

        return await userReports
            .Concat(threadReports)
            .Concat(postReports)
            .OrderByDescending(x => x.ReportCount)
            .ThenByDescending(x => x.LastReportedAt)
            .Take(topCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TopUserRow>> GetTopUsersRowsAsync(
        int topCount,
        CancellationToken cancellationToken = default)
    {
        if (topCount < 1) topCount = 10;
        if (topCount > 100) topCount = 100;

        // Thread/Post sayımlarını DB tarafında hesapla.
        var threadCounts = _context.Threads
            .GroupBy(t => t.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() });

        var postCounts = _context.Posts
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() });

        var query =
            from u in _context.Users
            join tc in threadCounts on u.Id equals tc.UserId into tcj
            from tc in tcj.DefaultIfEmpty()
            join pc in postCounts on u.Id equals pc.UserId into pcj
            from pc in pcj.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.Username,
                u.Email,
                ThreadCount = (int?)tc.Count ?? 0,
                PostCount = (int?)pc.Count ?? 0
            };

        return await query
            .OrderByDescending(x => x.ThreadCount + x.PostCount)
            .ThenBy(x => x.Id)
            .Take(topCount)
            .Select(x => new TopUserRow(
                x.Id,
                x.Username,
                x.Email,
                x.ThreadCount,
                x.PostCount))
            .ToListAsync(cancellationToken);
    }
}
