
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(
        int userId,
        int page = 1,
        int pageSize = 20,
        bool onlyUnread = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        System.Linq.Expressions.Expression<Func<Notifications, bool>> predicate = onlyUnread
            ? n => n.UserId == userId && !n.IsRead
            : n => n.UserId == userId;

        var (notifications, totalCount) = await _unitOfWork.Notifications.FindPagedAsync(
            predicate: predicate,
            include: query => query
                .Include(n => n.ActorUser)
                .Include(n => n.Thread)
                .Include(n => n.Post)!,
            orderBy: q => q.OrderByDescending(n => n.CreatedAt),
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        return new PagedResultDto<NotificationDto>
        {
            Items = notifications.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var unreadCount = await _unitOfWork.Notifications.CountAsync(
            n => n.UserId == userId && !n.IsRead,
            cancellationToken);

        var totalCount = await _unitOfWork.Notifications.CountAsync(
            n => n.UserId == userId,
            cancellationToken);

        var (lastNotifications, _) = await _unitOfWork.Notifications.FindPagedAsync(
            predicate: n => n.UserId == userId,
            orderBy: q => q.OrderByDescending(n => n.CreatedAt),
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        var lastNotificationDate = lastNotifications.FirstOrDefault()?.CreatedAt;

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            TotalCount = totalCount,
            LastNotificationDate = lastNotificationDate
        };
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Bildirimi getir
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);

        if (notification == null)
            return false;

        // 2. Güvenlik kontrolü: Bu bildirim bu kullanıcının mı?
        if (notification.UserId != userId)
        {
            _logger.LogWarning(
                "Kullanıcı {UserId} başka kullanıcının ({OwnerId}) bildirimini ({NotificationId}) okumaya çalıştı",
                userId, notification.UserId, notificationId);
            return false;
        }

        // 3. Zaten okunmuşsa tekrar güncelleme
        if (notification.IsRead)
            return true;

        // 4. Okundu olarak işaretle
        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bildirim {NotificationId} kullanıcı {UserId} tarafından okundu işaretlendi", 
            notificationId, userId);

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var unreadNotifications = (await _unitOfWork.Notifications.FindAsync(
            n => n.UserId == userId && !n.IsRead,
            cancellationToken)).ToList();

        if (!unreadNotifications.Any())
            return 0;

        // 2. Hepsini okundu yap
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        _unitOfWork.Notifications.UpdateRange(unreadNotifications);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} bildirim kullanıcı {UserId} için okundu işaretlendi", 
            unreadNotifications.Count, userId);

        return unreadNotifications.Count;
    }

    public async Task<bool> DeleteNotificationAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Bildirimi getir
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);

        if (notification == null)
            return false;

        // 2. Güvenlik kontrolü
        if (notification.UserId != userId)
        {
            _logger.LogWarning(
                "Kullanıcı {UserId} başka kullanıcının ({OwnerId}) bildirimini ({NotificationId}) silmeye çalıştı",
                userId, notification.UserId, notificationId);
            return false;
        }

        // 3. Soft delete
        _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bildirim {NotificationId} kullanıcı {UserId} tarafından silindi", 
            notificationId, userId);

        return true;
    }

    public async Task<int> CreateNotificationAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Notification entity oluştur
        var notification = new Notifications
        {
            UserId = dto.UserId,
            ActorUserId = dto.ActorUserId,
            Type = dto.Type,
            Message = dto.Message,
            ThreadId = dto.ThreadId,
            PostId = dto.PostId,
            IsRead = false
        };

        // 2. Veritabanına kaydet
        await _unitOfWork.Notifications.CreateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni bildirim oluşturuldu: User={UserId}, Actor={ActorUserId}, Type={Type}, Thread={ThreadId}, Post={PostId}",
            dto.UserId, dto.ActorUserId, dto.Type, dto.ThreadId, dto.PostId);

        return notification.Id;
    }

    // HELPER METHOD: Entity → DTO dönüşümü
    private static NotificationDto MapToDto(Notifications notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            
            // Actor bilgileri (nullable)
            ActorUserId = notification.ActorUserId,
            ActorUsername = notification.ActorUser?.Username,
            ActorFirstName = notification.ActorUser?.FirstName,
            ActorLastName = notification.ActorUser?.LastName,
            
            // İlgili içerik bilgileri
            ThreadId = notification.ThreadId,
            ThreadTitle = notification.Thread?.Title,
            PostId = notification.PostId
        };
    }
}
