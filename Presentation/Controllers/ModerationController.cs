using Application.DTOs.Moderation;
using Application.Services.Abstractions;
using Application.Validations.Moderation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Moderasyon işlemleri için controller (Ban, Mute, Lock Thread)
/// </summary>
public class ModerationController : AppController
{
    private readonly IModerationService _moderationService;
    private readonly IValidator<BanUserDto> _banUserValidator;
    private readonly IValidator<MuteUserDto> _muteUserValidator;
    private readonly ILogger<ModerationController> _logger;

    // CONSTRUCTOR - Servisleri inject ediyoruz (ASP.NET otomatik veriyor)
    public ModerationController(
        IModerationService moderationService,
        IValidator<BanUserDto> banUserValidator,
        IValidator<MuteUserDto> muteUserValidator,
        ILogger<ModerationController> logger)
    {
        _moderationService = moderationService;
        _banUserValidator = banUserValidator;
        _muteUserValidator = muteUserValidator;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcıyı yasaklar (geçici veya kalıcı)
    /// </summary>
    /// <param name="dto">Ban bilgileri (UserId, Reason, ExpiresAt)</param>
    /// <returns>Oluşturulan ban kaydı</returns>
    [HttpPost("ban")]
    [Authorize(Roles = "Admin")] // Sadece admin kullanabilir
    public async Task<IActionResult> BanUser([FromBody] BanUserDto dto)
    {
        var validationResult = await _banUserValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        // JWT'DEN KULLANICI ID'Sİ AL - Kim ban atıyor?
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            // SERVİS METHOD ÇAĞIR - İş mantığı burada
            var ban = await _moderationService.BanUserAsync(dto, adminUserId);
            
            // BAŞARILI SONUÇ DÖNDÜR
            _logger.LogInformation("User {UserId} banned by admin {AdminId}", dto.UserId, adminUserId);
            return Ok(ban);
        }
        catch (InvalidOperationException ex)
        {
            // İş mantığı hatası (kullanıcı bulunamadı, zaten ban var, vs.)
            _logger.LogWarning(ex, "Ban failed for user {UserId}", dto.UserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Yetki hatası (admin değil, vs.)
            _logger.LogWarning(ex, "Unauthorized ban attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            // Beklenmeyen hata
            _logger.LogError(ex, "Error banning user {UserId}", dto.UserId);
            return StatusCode(500, new { message = "Kullanıcı yasaklanırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kullanıcının yasağını kaldırır
    /// </summary>
    /// <param name="userId">Yasağı kaldırılacak kullanıcı ID'si</param>
    [HttpDelete("ban/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnbanUser(int userId)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            var result = await _moderationService.UnbanUserAsync(userId, adminUserId);
            _logger.LogInformation("User {UserId} unbanned by admin {AdminId}", userId, adminUserId);
            return Ok(new { message = "Kullanıcının yasağı başarıyla kaldırıldı." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unban failed for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized unban attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbanning user {UserId}", userId);
            return StatusCode(500, new { message = "Yasak kaldırılırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kullanıcının ban geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    [HttpGet("user/{userId}/bans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserBanHistory(int userId)
    {
        try
        {
            var bans = await _moderationService.GetUserBanHistoryAsync(userId);
            return Ok(bans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ban history for user {UserId}", userId);
            return StatusCode(500, new { message = "Ban geçmişi getirilirken bir hata oluştu." });
        }
    }

    // MUTE İŞLEMLERİ

    /// <summary>
    /// Kullanıcıyı susturur (geçici)
    /// </summary>
    /// <param name="dto">Mute bilgileri (UserId, Reason, ExpiresAt)</param>
    [HttpPost("mute")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MuteUser([FromBody] MuteUserDto dto)
    {
        var validationResult = await _muteUserValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            var mute = await _moderationService.MuteUserAsync(dto, adminUserId);
            _logger.LogInformation("User {UserId} muted by admin {AdminId}", dto.UserId, adminUserId);
            return Ok(mute);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Mute failed for user {UserId}", dto.UserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized mute attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error muting user {UserId}", dto.UserId);
            return StatusCode(500, new { message = "Kullanıcı susturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kullanıcının susturmasını kaldırır
    /// </summary>
    /// <param name="userId">Susturması kaldırılacak kullanıcı ID'si</param>
    [HttpDelete("mute/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnmuteUser(int userId)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            var result = await _moderationService.UnmuteUserAsync(userId, adminUserId);
            _logger.LogInformation("User {UserId} unmuted by admin {AdminId}", userId, adminUserId);
            return Ok(new { message = "Kullanıcının susturması başarıyla kaldırıldı." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unmute failed for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized unmute attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmuting user {UserId}", userId);
            return StatusCode(500, new { message = "Susturma kaldırılırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kullanıcının mute geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    [HttpGet("user/{userId}/mutes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserMuteHistory(int userId)
    {
        try
        {
            var mutes = await _moderationService.GetUserMuteHistoryAsync(userId);
            return Ok(mutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mute history for user {UserId}", userId);
            return StatusCode(500, new { message = "Susturma geçmişi getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Thread'i kilitler (yeni post eklenemez)
    /// </summary>
    /// <param name="threadId">Kilitlenecek thread ID'si</param>
    [HttpPut("thread/{threadId}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LockThread(int threadId)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            var result = await _moderationService.LockThreadAsync(threadId, adminUserId);
            _logger.LogInformation("Thread {ThreadId} locked by admin {AdminId}", threadId, adminUserId);
            return Ok(new { message = "Konu başarıyla kilitlendi." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Lock failed for thread {ThreadId}", threadId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized lock attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking thread {ThreadId}", threadId);
            return StatusCode(500, new { message = "Konu kilitlenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Thread kilidini açar
    /// </summary>
    /// <param name="threadId">Kilidi açılacak thread ID'si</param>
    [HttpPut("thread/{threadId}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnlockThread(int threadId)
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var adminUserId))
        {
            return Unauthorized(new { message = "Oturum bilgisi bulunamadı." });
        }

        try
        {
            var result = await _moderationService.UnlockThreadAsync(threadId, adminUserId);
            _logger.LogInformation("Thread {ThreadId} unlocked by admin {AdminId}", threadId, adminUserId);
            return Ok(new { message = "Konu kilidi başarıyla kaldırıldı." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unlock failed for thread {ThreadId}", threadId);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized unlock attempt by {AdminId}", adminUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking thread {ThreadId}", threadId);
            return StatusCode(500, new { message = "Konu kilidi kaldırılırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kullanıcı adı veya isim-soyisme göre kullanıcı arar (Admin paneli)
    /// </summary>
    /// <param name="q">Aranacak kelime (username, firstName, lastName)</param>
    /// <returns>Eşleşen kullanıcılar</returns>
    [HttpGet("search/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new { message = "Arama terimi en az 2 karakter olmalıdır." });
        }

        try
        {
            var users = await _moderationService.SearchUsersAsync(q);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with term: {SearchTerm}", q);
            return StatusCode(500, new { message = "Kullanıcı arama sırasında bir hata oluştu." });
        }
    }

    /// <summary>
    /// Thread başlığına göre thread arar (Admin paneli)
    /// </summary>
    /// <param name="q">Aranacak kelime (thread title)</param>
    /// <returns>Eşleşen thread'ler</returns>
    [HttpGet("search/threads")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchThreads([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new { message = "Arama terimi en az 2 karakter olmalıdır." });
        }

        try
        {
            var threads = await _moderationService.SearchThreadsAsync(q);
            return Ok(threads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching threads with term: {SearchTerm}", q);
            return StatusCode(500, new { message = "Konu arama sırasında bir hata oluştu." });
        }
    }
}
