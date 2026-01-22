using Application.DTOs.Common;
using Application.DTOs.Report;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReportsDto> CreateReportAsync(CreateReportDto dto, int reporterUserId)
    {
        _logger.LogInformation("Creating report by user {UserId}", reporterUserId);

        // Validation: En az bir hedef seçilmiş mi?
        if (!dto.ReportedUserId.HasValue && !dto.ReportedPostId.HasValue && !dto.ReportedThreadId.HasValue)
        {
            throw new InvalidOperationException("At least one target must be specified (User, Post, or Thread).");
        }

        // Validation: Hedefler gerçekten var mı?
        if (dto.ReportedUserId.HasValue)
        {
            var userExists = await _unitOfWork.Users.AnyAsync(u => u.Id == dto.ReportedUserId.Value);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {dto.ReportedUserId} not found.");
            }
        }

        if (dto.ReportedPostId.HasValue)
        {
            var postExists = await _unitOfWork.Posts.AnyAsync(p => p.Id == dto.ReportedPostId.Value);
            if (!postExists)
            {
                throw new InvalidOperationException($"Post with ID {dto.ReportedPostId} not found.");
            }
        }

        if (dto.ReportedThreadId.HasValue)
        {
            var threadExists = await _unitOfWork.Threads.AnyAsync(t => t.Id == dto.ReportedThreadId.Value);
            if (!threadExists)
            {
                throw new InvalidOperationException($"Thread with ID {dto.ReportedThreadId} not found.");
            }
        }

        // Entity oluştur
        var report = new Reports
        {
            ReporterId = reporterUserId,
            ReportedUserId = dto.ReportedUserId,
            ReportedPostId = dto.ReportedPostId,
            ReportedThreadId = dto.ReportedThreadId,
            Reason = dto.Reason,
            Description = dto.Description,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reports.CreateAsync(report);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} created successfully by user {UserId}", report.Id, reporterUserId);

        // DTO'ya dönüştür
        return await MapToReportsDtoAsync(report);
    }

    public async Task<PagedResultDto<ReportListDto>> GetMyReportsAsync(int userId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting reports for user {UserId}, Page: {Page}, PageSize: {PageSize}", 
            userId, page, pageSize);

        // GetAllWithIncludesAsync kullanarak tüm raporları al
        var allReports = await _unitOfWork.Reports.GetAllWithIncludesAsync(
            include: query => query
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedThread));

        // Filtreleme ve sıralama
        var filteredReports = allReports
            .Where(r => r.ReporterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var totalCount = filteredReports.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var reports = filteredReports
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = reports.Select(r => new ReportListDto
        {
            Id = r.Id,
            ReporterUsername = r.Reporter.Username,
            ReportedType = GetReportedType(r),
            ReportedInfo = GetReportedInfo(r),
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        });

        return new PagedResultDto<ReportListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<PagedResultDto<ReportListDto>> GetAllReportsAsync(ReportStatus? status = null, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting all reports, Status: {Status}, Page: {Page}, PageSize: {PageSize}", 
            status, page, pageSize);

        // GetAllWithIncludesAsync kullanarak tüm raporları al
        var allReports = await _unitOfWork.Reports.GetAllWithIncludesAsync(
            include: query => query
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedThread));

        // Filtreleme ve sıralama
        var filteredReports = allReports.AsEnumerable();
        
        if (status.HasValue)
        {
            filteredReports = filteredReports.Where(r => r.Status == status.Value);
        }

        filteredReports = filteredReports.OrderByDescending(r => r.CreatedAt);

        var reportsList = filteredReports.ToList();
        var totalCount = reportsList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var reports = reportsList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = reports.Select(r => new ReportListDto
        {
            Id = r.Id,
            ReporterUsername = r.Reporter.Username,
            ReportedType = GetReportedType(r),
            ReportedInfo = GetReportedInfo(r),
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        });

        return new PagedResultDto<ReportListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<ReportsDto> GetReportByIdAsync(int reportId, int requestingUserId)
    {
        _logger.LogInformation("Getting report {ReportId} for user {UserId}", reportId, requestingUserId);

        // GetAllWithIncludesAsync kullanarak raporu bul
        var allReports = await _unitOfWork.Reports.GetAllWithIncludesAsync(
            include: query => query
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedThread)
                .Include(r => r.ReviewedByUser));

        var report = allReports.FirstOrDefault(r => r.Id == reportId);

        if (report == null)
        {
            throw new InvalidOperationException($"Report with ID {reportId} not found.");
        }

        // Güvenlik kontrolü: Sadece rapor sahibi kendi raporunu görebilir
        // (Admin kontrolü controller'da [Authorize(Roles = "Admin")] ile yapılacak)
        var requestingUser = await _unitOfWork.Users.GetByIdAsync(requestingUserId);
        if (requestingUser == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (report.ReporterId != requestingUserId && requestingUser.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("You can only view your own reports.");
        }

        return await MapToReportsDtoAsync(report);
    }

    public async Task<ReportsDto> UpdateReportStatusAsync(int reportId, UpdateReportStatusDto dto, int adminUserId)
    {
        _logger.LogInformation("Updating report {ReportId} status to {Status} by admin {AdminId}", 
            reportId, dto.Status, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can update report status.");
        }

        var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            throw new InvalidOperationException($"Report with ID {reportId} not found.");
        }

        // Status güncelle
        report.Status = dto.Status;
        report.ReviewedByUserId = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.AdminNote = dto.AdminNote;
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} status updated successfully to {Status}", reportId, dto.Status);

        // Güncellenmiş raporu döndür
        return await GetReportByIdAsync(reportId, adminUserId);
    }

    public async Task<bool> DeleteReportAsync(int reportId, int userId)
    {
        _logger.LogInformation("Deleting report {ReportId} by user {UserId}", reportId, userId);

        var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            throw new InvalidOperationException($"Report with ID {reportId} not found.");
        }

        // Güvenlik: Sadece admin silebilir
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can delete reports.");
        }

        // Soft delete
        _unitOfWork.Reports.Delete(report);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} deleted successfully", reportId);

        return true;
    }

    // Helper methods
    private async Task<ReportsDto> MapToReportsDtoAsync(Reports report)
    {
        // Eğer navigation property'ler yüklenmemişse yükle
        if (report.Reporter == null)
        {
            var reporter = await _unitOfWork.Users.GetByIdAsync(report.ReporterId);
            if (reporter != null) report.Reporter = reporter;
        }

        if (report.ReportedUserId.HasValue && report.ReportedUser == null)
        {
            var reportedUser = await _unitOfWork.Users.GetByIdAsync(report.ReportedUserId.Value);
            if (reportedUser != null) report.ReportedUser = reportedUser;
        }

        if (report.ReportedPostId.HasValue && report.ReportedPost == null)
        {
            var reportedPost = await _unitOfWork.Posts.GetByIdAsync(report.ReportedPostId.Value);
            if (reportedPost != null) report.ReportedPost = reportedPost;
        }

        if (report.ReportedThreadId.HasValue && report.ReportedThread == null)
        {
            var reportedThread = await _unitOfWork.Threads.GetByIdAsync(report.ReportedThreadId.Value);
            if (reportedThread != null) report.ReportedThread = reportedThread;
        }

        if (report.ReviewedByUserId.HasValue && report.ReviewedByUser == null)
        {
            var reviewedBy = await _unitOfWork.Users.GetByIdAsync(report.ReviewedByUserId.Value);
            if (reviewedBy != null) report.ReviewedByUser = reviewedBy;
        }

        return new ReportsDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterUsername = report.Reporter?.Username ?? string.Empty,
            ReporterEmail = report.Reporter?.Email ?? string.Empty,
            ReportedUserId = report.ReportedUserId,
            ReportedUsername = report.ReportedUser?.Username,
            ReportedPostId = report.ReportedPostId,
            PostTitle = report.ReportedPost?.Content?.Substring(0, Math.Min(100, report.ReportedPost.Content.Length)),
            ReportedThreadId = report.ReportedThreadId,
            ThreadTitle = report.ReportedThread?.Title,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedByUsername = report.ReviewedByUser?.Username,
            ReviewedAt = report.ReviewedAt,
            AdminNote = report.AdminNote,
            CreatedAt = report.CreatedAt
        };
    }

    private string GetReportedType(Reports report)
    {
        if (report.ReportedUserId.HasValue) return "User";
        if (report.ReportedPostId.HasValue) return "Post";
        if (report.ReportedThreadId.HasValue) return "Thread";
        return "Unknown";
    }

    private string GetReportedInfo(Reports report)
    {
        if (report.ReportedUser != null) return report.ReportedUser.Username;
        if (report.ReportedPost != null) return report.ReportedPost.Content?.Substring(0, Math.Min(50, report.ReportedPost.Content.Length)) ?? "Post";
        if (report.ReportedThread != null) return report.ReportedThread.Title;
        return "N/A";
    }
}
