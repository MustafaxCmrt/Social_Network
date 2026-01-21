using Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Bildirim yönetimi API controller
/// Kullanıcılar kendi bildirimlerini görüntüleyebilir, okundu işaretleyebilir ve silebilir
/// </summary>
[Authorize] // Tüm endpoint'ler authentication gerektirir
public class NotificationController : AppController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Giriş yapan kullanıcının bildirimlerini getirir (sayfalama ile)
    /// </summary>
    /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa boyutu (varsayılan: 20, max: 50)</param>
    /// <param name="onlyUnread">Sadece okunmamışları getir (varsayılan: false)</param>
    /// <returns>200 OK - Bildirim listesi</returns>
    [HttpGet("my")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyUnread = false)
    {
        // 1. Token'dan kullanıcı ID'sini al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 2. Parametreleri doğrula
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        // 3. Bildirimleri getir
        var notifications = await _notificationService.GetMyNotificationsAsync(
            currentUserId,
            page,
            pageSize,
            onlyUnread,
            HttpContext.RequestAborted);

        return Ok(notifications);
    }

    /// <summary>
    /// Bildirim özeti getirir (okunmamış bildirim sayısı - badge için)
    /// </summary>
    /// <returns>200 OK - Bildirim özeti</returns>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotificationSummary()
    {
        // 1. Token'dan kullanıcı ID'sini al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 2. Özeti getir
        var summary = await _notificationService.GetNotificationSummaryAsync(
            currentUserId,
            HttpContext.RequestAborted);

        return Ok(summary);
    }

    /// <summary>
    /// Bir bildirimi okundu olarak işaretler
    /// </summary>
    /// <param name="id">Bildirim ID</param>
    /// <returns>
    /// 200 OK - İşlem başarılı
    /// 404 Not Found - Bildirim bulunamadı veya yetki yok
    /// </returns>
    [HttpPut("{id}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        // 1. Token'dan kullanıcı ID'sini al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 2. Bildirimi okundu işaretle
        var result = await _notificationService.MarkAsReadAsync(
            id,
            currentUserId,
            HttpContext.RequestAborted);

        if (!result)
        {
            return NotFound(new { message = "Bildirim bulunamadı veya bu bildirimi okuma yetkiniz yok" });
        }

        return Ok(new { message = "Bildirim okundu olarak işaretlendi" });
    }

    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretler
    /// </summary>
    /// <returns>200 OK - İşleme alınan bildirim sayısı</returns>
    [HttpPut("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        // 1. Token'dan kullanıcı ID'sini al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 2. Tüm bildirimleri okundu işaretle
        var count = await _notificationService.MarkAllAsReadAsync(
            currentUserId,
            HttpContext.RequestAborted);

        return Ok(new
        {
            message = "Tüm bildirimler okundu olarak işaretlendi",
            count
        });
    }

    /// <summary>
    /// Bir bildirimi siler
    /// </summary>
    /// <param name="id">Bildirim ID</param>
    /// <returns>
    /// 200 OK - Silme başarılı
    /// 404 Not Found - Bildirim bulunamadı veya yetki yok
    /// </returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        // 1. Token'dan kullanıcı ID'sini al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 2. Bildirimi sil
        var result = await _notificationService.DeleteNotificationAsync(
            id,
            currentUserId,
            HttpContext.RequestAborted);

        if (!result)
        {
            return NotFound(new { message = "Bildirim bulunamadı veya bu bildirimi silme yetkiniz yok" });
        }

        return Ok(new { message = "Bildirim başarıyla silindi" });
    }
}
