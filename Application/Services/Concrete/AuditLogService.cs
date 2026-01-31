using Application.DTOs.AuditLog;
using Application.DTOs.Common;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Audit log servisi implementation
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IUnitOfWork unitOfWork, ILogger<AuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuditLogDto> CreateLogAsync(CreateAuditLogDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLogs
            {
                UserId = dto.UserId,
                Username = dto.Username,
                Action = dto.Action,
                EntityType = dto.EntityType,
                EntityId = dto.EntityId,
                OldValue = dto.OldValue,
                NewValue = dto.NewValue,
                IpAddress = dto.IpAddress,
                UserAgent = dto.UserAgent,
                Success = dto.Success,
                ErrorMessage = dto.ErrorMessage,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.CreateAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToDto(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for action {Action}", dto.Action);
            throw;
        }
    }

    public async Task<PagedResultDto<AuditLogListDto>> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default)
    {
        try
        {
            // Filtreleri DB tarafında uygula
            var userId = filter.UserId;
            var action = filter.Action?.ToLower();
            var entityType = filter.EntityType;
            var entityId = filter.EntityId;
            var success = filter.Success;
            var startDate = filter.StartDate;
            var endDate = filter.EndDate;

            var (logs, totalCount) = await _unitOfWork.AuditLogs.FindPagedAsync(
                predicate: l =>
                    (!userId.HasValue || l.UserId == userId.Value) &&
                    (action == null || l.Action.ToLower().Contains(action)) &&
                    (entityType == null || l.EntityType == entityType) &&
                    (!entityId.HasValue || l.EntityId == entityId.Value) &&
                    (!success.HasValue || l.Success == success.Value) &&
                    (!startDate.HasValue || l.CreatedAt >= startDate.Value) &&
                    (!endDate.HasValue || l.CreatedAt <= endDate.Value),
                orderBy: q => q.OrderByDescending(l => l.CreatedAt),
                page: filter.Page,
                pageSize: filter.PageSize,
                cancellationToken: cancellationToken);

            var pagedLogs = logs.Select(MapToListDto).ToList();

            return new PagedResultDto<AuditLogListDto>
            {
                Items = pagedLogs,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            throw;
        }
    }

    public async Task<AuditLogDto?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _unitOfWork.AuditLogs.GetByIdAsync(id, cancellationToken);
            return log != null ? MapToDto(log) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log {Id}", id);
            throw;
        }
    }

    public async Task<PagedResultDto<AuditLogListDto>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilterDto
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        };

        return await GetLogsAsync(filter, cancellationToken);
    }

    public async Task<PagedResultDto<AuditLogListDto>> GetEntityHistoryAsync(string entityType, int entityId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilterDto
        {
            EntityType = entityType,
            EntityId = entityId,
            Page = page,
            PageSize = pageSize
        };

        return await GetLogsAsync(filter, cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(int days, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var oldLogs = await _unitOfWork.AuditLogs.FindAsync(l => l.CreatedAt < cutoffDate, cancellationToken);

            var logList = oldLogs.ToList();
            if (!logList.Any())
                return 0;

            _unitOfWork.AuditLogs.DeleteRange(logList); // Soft delete
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} audit logs older than {Days} days", logList.Count, days);
            return logList.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old audit logs");
            throw;
        }
    }

    #region Mapping

    private static AuditLogDto MapToDto(AuditLogs log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            Username = log.Username,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt
        };
    }

    private static AuditLogListDto MapToListDto(AuditLogs log)
    {
        return new AuditLogListDto
        {
            Id = log.Id,
            Username = log.Username,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Success = log.Success,
            CreatedAt = log.CreatedAt
        };
    }

    #endregion
}
