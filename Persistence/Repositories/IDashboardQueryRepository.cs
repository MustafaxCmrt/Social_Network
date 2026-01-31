using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories;

public record TopReportedContentRow(
    string ContentType,
    int ContentId,
    int ReportCount,
    DateTime LastReportedAt);

public record TopUserRow(
    int UserId,
    string Username,
    string Email,
    int ThreadCount,
    int PostCount)
{
    public int TotalActivity => ThreadCount + PostCount;
}

public interface IDashboardQueryRepository
{
    Task<List<TopReportedContentRow>> GetTopReportedContentRowsAsync(
        int topCount,
        CancellationToken cancellationToken = default);

    Task<List<TopUserRow>> GetTopUsersRowsAsync(
        int topCount,
        CancellationToken cancellationToken = default);
}
