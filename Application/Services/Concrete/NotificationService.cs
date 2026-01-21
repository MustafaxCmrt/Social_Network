using Application.Common.Extensions;
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
        // 1. Kullanıcının bildirimlerini getir (ilişkilerle birlikte)
        var notifications = await _unitOfWork.Notifications.GetAllWithIncludesAsync(
            include: query => query
                .Include(n => n.ActorUser)      // Kim yaptı?
                .Include(n => n.Thread)         // Hangi thread?
                .Include(n => n.Post),          // Hangi post?
            cancellationToken);

        // 2. Kullanıcıya ait olanları filtrele
        var userNotifications = notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        // 3. Sadece okunmamışlar isteniyorsa filtrele
        if (onlyUnread)
        {
            userNotifications = userNotifications.Where(n => !n.IsRead);
        }

        // 4. Tarihe göre sırala (en yeni en üstte)
        var ordered = userNotifications.OrderByDescending(n => n.CreatedAt);

        // 5. Sayfalama ve DTO'ya dönüştürme (PaginationExtensions kullanarak)
        return ordered.ToPagedResult(page, pageSize, MapToDto);
    }

    public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _unitOfWork.Notifications.GetAllAsync(cancellationToken);

        var userNotifications = notifications.Where(n => n.UserId == userId).ToList();

        return new NotificationSummaryDto
        {
            UnreadCount = userNotifications.Count(n => !n.IsRead),
            TotalCount = userNotifications.Count,
            LastNotificationDate = userNotifications
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefault()?.CreatedAt
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
        // 1. Kullanıcının okunmamış bildirimlerini getir
        var notifications = await _unitOfWork.Notifications.GetAllAsync(cancellationToken);
        var unreadNotifications = notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToList();

        if (!unreadNotifications.Any())
            return 0;

        // 2. Hepsini okundu yap
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

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
