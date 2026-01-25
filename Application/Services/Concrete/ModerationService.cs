using Application.DTOs.Moderation;
using Application.DTOs.AuditLog;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

public class ModerationService : IModerationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ModerationService> _logger;

    public ModerationService(
        IUnitOfWork unitOfWork, 
        IAuditLogService auditLogService,
        ILogger<ModerationService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<UserBanDto> BanUserAsync(BanUserDto dto, int adminUserId)
    {
        _logger.LogInformation("Banning user {UserId} by admin {AdminId}", dto.UserId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can ban users.");
        }

        // Kullanıcı var mı?
        var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {dto.UserId} not found.");
        }

        // Admin kendini yasaklayamaz
        if (dto.UserId == adminUserId)
        {
            throw new InvalidOperationException("You cannot ban yourself.");
        }

        // Başka bir admin'i yasaklayamaz
        if (user.Role == Roles.Admin)
        {
            throw new InvalidOperationException("You cannot ban another admin.");
        }

        // Aktif ban var mı kontrol et
        var existingBans = await _unitOfWork.UserBans.FindAsync(b => 
            b.UserId == dto.UserId && b.IsActive);
        var activeBan = existingBans.FirstOrDefault();

        if (activeBan != null)
        {
            throw new InvalidOperationException($"User is already banned. Ban expires at: {activeBan.ExpiresAt?.ToString() ?? "Never"}");
        }

        // Yeni ban oluştur
        var ban = new UserBans
        {
            UserId = dto.UserId,
            BannedByUserId = adminUserId,
            Reason = dto.Reason,
            BannedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserBans.CreateAsync(ban);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} banned successfully by admin {AdminId}. Expires: {ExpiresAt}", 
            dto.UserId, adminUserId, dto.ExpiresAt?.ToString() ?? "Never");

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "BanUser",
            EntityType = "User",
            EntityId = dto.UserId,
            NewValue = $"Banned until {dto.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ?? "Permanent"}. Reason: {dto.Reason}",
            Success = true
        });

        // DTO'ya dönüştür
        return new UserBanDto
        {
            Id = ban.Id,
            UserId = ban.UserId,
            Username = user.Username,
            BannedByUserId = ban.BannedByUserId,
            BannedByUsername = admin.Username,
            Reason = ban.Reason,
            BannedAt = ban.BannedAt,
            ExpiresAt = ban.ExpiresAt,
            IsActive = ban.IsActive
        };
    }

    public async Task<bool> UnbanUserAsync(int userId, int adminUserId)
    {
        _logger.LogInformation("Unbanning user {UserId} by admin {AdminId}", userId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can unban users.");
        }

        // Aktif ban bul
        var existingBans = await _unitOfWork.UserBans.FindAsync(b => 
            b.UserId == userId && b.IsActive);
        var activeBan = existingBans.FirstOrDefault();

        if (activeBan == null)
        {
            throw new InvalidOperationException("User is not currently banned.");
        }

        // Ban'ı deaktif et
        activeBan.IsActive = false;
        activeBan.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserBans.Update(activeBan);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unbanned successfully by admin {AdminId}", userId, adminUserId);

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "UnbanUser",
            EntityType = "User",
            EntityId = userId,
            OldValue = $"Banned (Reason: {activeBan.Reason})",
            NewValue = "Unbanned",
            Success = true
        });

        return true;
    }

    public async Task<UserMuteDto> MuteUserAsync(MuteUserDto dto, int adminUserId)
    {
        _logger.LogInformation("Muting user {UserId} by admin {AdminId}", dto.UserId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can mute users.");
        }

        // Kullanıcı var mı?
        var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {dto.UserId} not found.");
        }

        // Admin kendini susturamaz
        if (dto.UserId == adminUserId)
        {
            throw new InvalidOperationException("You cannot mute yourself.");
        }

        // Başka bir admin'i susturamaz
        if (user.Role == Roles.Admin)
        {
            throw new InvalidOperationException("You cannot mute another admin.");
        }

        // Aktif mute var mı kontrol et
        var existingMutes = await _unitOfWork.UserMutes.FindAsync(m => 
            m.UserId == dto.UserId && m.IsActive);
        var activeMute = existingMutes.FirstOrDefault();

        if (activeMute != null)
        {
            throw new InvalidOperationException($"User is already muted. Mute expires at: {activeMute.ExpiresAt}");
        }

        // Yeni mute oluştur
        var mute = new UserMutes
        {
            UserId = dto.UserId,
            MutedByUserId = adminUserId,
            Reason = dto.Reason,
            MutedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserMutes.CreateAsync(mute);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} muted successfully by admin {AdminId}. Expires: {ExpiresAt}", 
            dto.UserId, adminUserId, dto.ExpiresAt);
        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "MuteUser",
            EntityType = "User",
            EntityId = dto.UserId,
            NewValue = $"Muted until {dto.ExpiresAt:yyyy-MM-dd HH:mm}. Reason: {dto.Reason}",
            Success = true
        });
        // DTO'ya dönüştür
        return new UserMuteDto
        {
            Id = mute.Id,
            UserId = mute.UserId,
            Username = user.Username,
            MutedByUserId = mute.MutedByUserId,
            MutedByUsername = admin.Username,
            Reason = mute.Reason,
            MutedAt = mute.MutedAt,
            ExpiresAt = mute.ExpiresAt,
            IsActive = mute.IsActive
        };
    }

    public async Task<bool> UnmuteUserAsync(int userId, int adminUserId)
    {
        _logger.LogInformation("Unmuting user {UserId} by admin {AdminId}", userId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can unmute users.");
        }

        // Aktif mute bul
        var existingMutes = await _unitOfWork.UserMutes.FindAsync(m => 
            m.UserId == userId && m.IsActive);
        var activeMute = existingMutes.FirstOrDefault();

        if (activeMute == null)
        {
            throw new InvalidOperationException("User is not currently muted.");
        }

        // Mute'u deaktif et
        activeMute.IsActive = false;
        activeMute.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserMutes.Update(activeMute);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unmuted successfully by admin {AdminId}", userId, adminUserId);

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "UnmuteUser",
            EntityType = "User",
            EntityId = userId,
            OldValue = $"Muted (Reason: {activeMute.Reason})",
            NewValue = "Unmuted",
            Success = true
        });

        return true;
    }

    public async Task<bool> LockThreadAsync(int threadId, int adminUserId)
    {
        _logger.LogInformation("Locking thread {ThreadId} by admin {AdminId}", threadId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can lock threads.");
        }

        // Thread var mı?
        var thread = await _unitOfWork.Threads.GetByIdAsync(threadId);
        if (thread == null)
        {
            throw new InvalidOperationException($"Thread with ID {threadId} not found.");
        }

        // Zaten kilitli mi?
        if (thread.IsLocked)
        {
            throw new InvalidOperationException("Thread is already locked.");
        }

        // Thread'i kilitle
        thread.IsLocked = true;
        thread.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Threads.Update(thread);
        await _unitOfWork.SaveChangesAsync();

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "LockThread",
            EntityType = "Thread",
            EntityId = threadId,
            OldValue = $"Title: {thread.Title}, IsLocked: false",
            NewValue = $"Title: {thread.Title}, IsLocked: true",
            Success = true
        });

        _logger.LogInformation("Thread {ThreadId} locked successfully by admin {AdminId}", threadId, adminUserId);

        return true;
    }

    public async Task<bool> UnlockThreadAsync(int threadId, int adminUserId)
    {
        _logger.LogInformation("Unlocking thread {ThreadId} by admin {AdminId}", threadId, adminUserId);

        // Admin kontrolü
        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        if (admin == null || admin.Role != Roles.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can unlock threads.");
        }

        // Thread var mı?
        var thread = await _unitOfWork.Threads.GetByIdAsync(threadId);
        if (thread == null)
        {
            throw new InvalidOperationException($"Thread with ID {threadId} not found.");
        }

        // Zaten açık mı?
        if (!thread.IsLocked)
        {
            throw new InvalidOperationException("Thread is not locked.");
        }

        // Thread kilidini aç
        thread.IsLocked = false;
        thread.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Threads.Update(thread);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Thread {ThreadId} unlocked successfully by admin {AdminId}", threadId, adminUserId);
        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            UserId = adminUserId,
            Username = admin.Username,
            Action = "UnlockThread",
            EntityType = "Thread",
            EntityId = threadId,
            OldValue = "Locked",
            NewValue = "Unlocked",
            Success = true
        });
        return true;
    }

    public async Task<(bool IsBanned, UserBanDto? ActiveBan)> IsUserBannedAsync(int userId)
    {
        var existingBans = await _unitOfWork.UserBans.GetAllWithIncludesAsync(
            include: query => query
                .Include(b => b.User)
                .Include(b => b.BannedByUser));

        var activeBan = existingBans
            .Where(b => b.UserId == userId && b.IsActive)
            .Where(b => b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow) // Süresi dolmamış veya kalıcı
            .FirstOrDefault();

        if (activeBan == null)
        {
            return (false, null);
        }

        var banDto = new UserBanDto
        {
            Id = activeBan.Id,
            UserId = activeBan.UserId,
            Username = activeBan.User?.Username ?? string.Empty,
            BannedByUserId = activeBan.BannedByUserId,
            BannedByUsername = activeBan.BannedByUser?.Username ?? string.Empty,
            Reason = activeBan.Reason,
            BannedAt = activeBan.BannedAt,
            ExpiresAt = activeBan.ExpiresAt,
            IsActive = activeBan.IsActive
        };

        return (true, banDto);
    }

    public async Task<(bool IsMuted, UserMuteDto? ActiveMute)> IsUserMutedAsync(int userId)
    {
        var existingMutes = await _unitOfWork.UserMutes.GetAllWithIncludesAsync(
            include: query => query
                .Include(m => m.User)
                .Include(m => m.MutedByUser));

        var activeMute = existingMutes
            .Where(m => m.UserId == userId && m.IsActive)
            .Where(m => m.ExpiresAt > DateTime.UtcNow) // Süresi dolmamış
            .FirstOrDefault();

        if (activeMute == null)
        {
            return (false, null);
        }

        var muteDto = new UserMuteDto
        {
            Id = activeMute.Id,
            UserId = activeMute.UserId,
            Username = activeMute.User?.Username ?? string.Empty,
            MutedByUserId = activeMute.MutedByUserId,
            MutedByUsername = activeMute.MutedByUser?.Username ?? string.Empty,
            Reason = activeMute.Reason,
            MutedAt = activeMute.MutedAt,
            ExpiresAt = activeMute.ExpiresAt,
            IsActive = activeMute.IsActive
        };

        return (true, muteDto);
    }

    public async Task<IEnumerable<UserBanDto>> GetUserBanHistoryAsync(int userId)
    {
        var bans = await _unitOfWork.UserBans.GetAllWithIncludesAsync(
            include: query => query
                .Include(b => b.User)
                .Include(b => b.BannedByUser));

        return bans
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BannedAt)
            .Select(b => new UserBanDto
            {
                Id = b.Id,
                UserId = b.UserId,
                Username = b.User?.Username ?? string.Empty,
                BannedByUserId = b.BannedByUserId,
                BannedByUsername = b.BannedByUser?.Username ?? string.Empty,
                Reason = b.Reason,
                BannedAt = b.BannedAt,
                ExpiresAt = b.ExpiresAt,
                IsActive = b.IsActive
            });
    }

    public async Task<IEnumerable<UserMuteDto>> GetUserMuteHistoryAsync(int userId)
    {
        var mutes = await _unitOfWork.UserMutes.GetAllWithIncludesAsync(
            include: query => query
                .Include(m => m.User)
                .Include(m => m.MutedByUser));

        return mutes
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MutedAt)
            .Select(m => new UserMuteDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Username = m.User?.Username ?? string.Empty,
                MutedByUserId = m.MutedByUserId,
                MutedByUsername = m.MutedByUser?.Username ?? string.Empty,
                Reason = m.Reason,
                MutedAt = m.MutedAt,
                ExpiresAt = m.ExpiresAt,
                IsActive = m.IsActive
            });
    }
}
