using Application.DTOs.User;
using Application.Services.Abstractions;
using Domain.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Kullanıcı yönetimi işlemleri için API controller
/// CRUD işlemlerini yönetir
/// </summary>
[Authorize] // Tüm endpoint'ler authentication gerektirir
public class UserController : AppController
{
    private readonly IUserService _userService;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserController> _logger;
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;
    private readonly IValidator<UpdateMyProfileDto> _updateMyProfileValidator;

    public UserController(
        IUserService userService,
        IFileService fileService,
        ICurrentUserService currentUserService,
        ILogger<UserController> logger,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator,
        IValidator<UpdateMyProfileDto> updateMyProfileValidator)
    {
        _userService = userService;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateMyProfileValidator = updateMyProfileValidator;
    }

    /// <summary>
    /// Yeni kullanıcı oluşturur (Sadece Admin)
    /// </summary>
    /// <param name="request">Oluşturulacak kullanıcı bilgileri</param>
    /// <returns>
    /// 201 Created - Kullanıcı oluşturuldu
    /// 400 Bad Request - Validation hatası
    /// 403 Forbidden - Yetki yok
    /// 409 Conflict - Username veya Email zaten kullanılıyor
    /// </returns>
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateUserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        // 1. Validation
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

        // 2. Token'dan currentUserId al (Admin)
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        // 3. Kullanıcı oluştur
        var result = await _userService.CreateUserAsync(request, currentUserId, HttpContext.RequestAborted);

        // 4. Username veya Email zaten kullanılıyor
        if (result == null)
        {
            return Conflict(new
            {
                Message = "Kullanıcı adı veya email adresi zaten kullanılıyor"
            });
        }

        // 5. Başarılı - 201 Created
        return CreatedAtAction(
            nameof(GetUserById),
            new { id = result.UserId },
            result
        );
    }

    /// <summary>
    /// ID'ye göre kullanıcı bilgilerini getirir
    /// Kullanıcı kendi profilini görebilir, Admin herkesin profilini görebilir
    /// </summary>
    /// <param name="id">Kullanıcı ID'si</param>
    /// <returns>
    /// 200 OK - Kullanıcı bilgileri
    /// 403 Forbidden - Yetki yok
    /// 404 Not Found - Kullanıcı bulunamadı
    /// </returns>
    [HttpGet("get/{id}")]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int id)
    {
        // 1. Token'dan currentUserId ve role al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        bool isAdmin = userRole == "Admin";

        // 2. Yetki kontrolü - Kullanıcı sadece kendi profilini görebilir (Admin hariç)
        if (!isAdmin && currentUserId != id)
        {
            return Forbid(); // 403 Forbidden
        }

        // 3. Kullanıcıyı getir
        var user = await _userService.GetUserByIdAsync(id, HttpContext.RequestAborted);

        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Sadece Admin) - Pagination ve search destekli
    /// </summary>
    /// <param name="page">Sayfa numarası (default: 1)</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı (default: 10, max: 100)</param>
    /// <param name="search">Arama terimi (username, firstName, lastName, email)</param>
    /// <returns>
    /// 200 OK - Sayfalanmış kullanıcı listesi
    /// 403 Forbidden - Yetki yok
    /// </returns>
    [HttpGet("getAll")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Application.DTOs.Common.PagedResultDto<UserListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var users = await _userService.GetAllUsersAsync(page, pageSize, search, HttpContext.RequestAborted);
        return Ok(users);
    }

    /// <summary>
    /// Kullanıcı bilgilerini günceller
    /// Kullanıcı kendi profilini güncelleyebilir, Admin herkesin profilini güncelleyebilir
    /// </summary>
    /// <param name="request">Güncellenmiş bilgiler (UserId dahil)</param>
    /// <returns>
    /// 200 OK - Güncelleme başarılı
    /// 400 Bad Request - Validation hatası
    /// 403 Forbidden - Yetki yok
    /// 404 Not Found - Kullanıcı bulunamadı
    /// 409 Conflict - Username veya Email zaten kullanılıyor
    /// </returns>
    [HttpPut("update")]
    [ProducesResponseType(typeof(UpdateUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto request)
    {
        // 1. Validation
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

        // 2. Token'dan currentUserId ve role al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        bool isAdmin = userRole == "Admin";

        // 3. Kullanıcıyı güncelle - request.UserId kullan
        var result = await _userService.UpdateUserAsync(request.UserId, request, currentUserId, isAdmin, HttpContext.RequestAborted);

        // 4. Sonuç kontrolü
        if (result == null)
        {
            // Kullanıcı bulunamadı veya Username/Email çakışması
            return Conflict(new
            {
                Message = "Kullanıcı bulunamadı, yetkiniz yok veya username/email zaten kullanılıyor"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcıyı siler (Soft Delete - Sadece Admin)
    /// </summary>
    /// <param name="id">Silinecek kullanıcının ID'si</param>
    /// <returns>
    /// 200 OK - Silme başarılı
    /// 403 Forbidden - Yetki yok
    /// 404 Not Found - Kullanıcı bulunamadı
    /// </returns>
    [HttpDelete("delete")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Token'dan currentUserId al (Admin)
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        var result = await _userService.DeleteUserAsync(id, currentUserId, HttpContext.RequestAborted);

        if (!result)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(new { message = "Kullanıcı başarıyla silindi" });
    }

    /// <summary>
    /// Giriş yapan kullanıcının kendi profil bilgilerini getirir
    /// </summary>
    /// <returns>200 OK - Kendi profil bilgileri</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        var user = await _userService.GetMyProfileAsync(currentUserId, HttpContext.RequestAborted);

        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Public kullanıcı profili getirir (thread/post istatistikleriyle)
    /// </summary>
    /// <param name="id">Kullanıcı ID'si</param>
    /// <returns>200 OK - Public profil</returns>
    [HttpGet("profile/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        var profile = await _userService.GetUserProfileAsync(id, HttpContext.RequestAborted);

        if (profile == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Giriş yapan kullanıcının kendi profilini günceller
    /// </summary>
    /// <param name="request">Güncellenmiş profil bilgileri</param>
    /// <returns>200 OK - Güncellenmiş profil</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto request)
    {
        // 1. Validation
        var validationResult = await _updateMyProfileValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }

        // 2. Token'dan currentUserId al
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        try
        {
            var result = await _userService.UpdateMyProfileAsync(currentUserId, request, HttpContext.RequestAborted);

            if (result == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kullanıcının kendi hesabını siler (soft delete + email/username suffix)
    /// </summary>
    /// <returns>200 OK - Hesap silindi</returns>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var currentUserIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Geçersiz token" });

        var result = await _userService.DeleteMyAccountAsync(currentUserId, HttpContext.RequestAborted);

        if (!result)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı veya zaten silinmiş" });
        }

        return Ok(new { message = "Hesabınız başarıyla silindi" });
    }

    /// <summary>
    /// Profil resmi yükler
    /// </summary>
    /// <param name="file">Yüklenecek profil resmi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Resim başarıyla yüklendi, URL döner
    /// 400 Bad Request - Geçersiz dosya (boyut/uzantı)
    /// 401 Unauthorized - Kullanıcı giriş yapmamış
    /// </returns>
    [HttpPost("upload-profile-image")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfileImage(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Dosya kontrolü
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Dosya yüklenemedi" });
            }

            // 2. Uzantı kontrolü
            if (!_fileService.IsValidImageExtension(file.FileName))
            {
                return BadRequest(new { message = "Sadece .jpg, .jpeg, .png, .gif uzantılı dosyalar yüklenebilir" });
            }

            // 3. Boyut kontrolü
            if (!_fileService.IsValidFileSize(file.Length))
            {
                return BadRequest(new { message = "Dosya boyutu maksimum 5 MB olabilir" });
            }

            // 4. Dosyayı yükle
            var imageUrl = await _fileService.UploadImageAsync(file, "profiles", cancellationToken);

            // 5. Kullanıcı ID'sini al
            var userId = _currentUserService.GetCurrentUserId();
            
            if (userId == null)
            {
                return Unauthorized(new { message = "Kullanıcı oturumu bulunamadı" });
            }

            // 6. Profil resmini veritabanına kaydet
            var updatedImageUrl = await _userService.UpdateProfileImageAsync(userId.Value, imageUrl, cancellationToken);

            if (updatedImageUrl == null)
            {
                _logger.LogWarning("Profil resmi veritabanına kaydedilemedi - Kullanıcı ID: {UserId}", userId);
                return BadRequest(new { message = "Profil resmi veritabanına kaydedilemedi" });
            }

            _logger.LogInformation("Profil resmi yüklendi ve veritabanına kaydedildi: {ImageUrl}, Kullanıcı ID: {UserId}", imageUrl, userId);

            return Ok(new { imageUrl = updatedImageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profil resmi yükleme hatası");
            return BadRequest(new { message = "Resim yüklenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Kullanıcının oluşturduğu thread'leri getirir (sayfalama ile)
    /// </summary>
    /// <param name="id">Kullanıcı ID'si</param>
    /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 10)</param>
    /// <returns>200 OK - Thread geçmişi</returns>
    [HttpGet("{id}/threads")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserThreadHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserThreads(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Sayfalama parametrelerini doğrula
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        var result = await _userService.GetUserThreadsAsync(id, pageNumber, pageSize, HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının yazdığı post'ları getirir (sayfalama ile)
    /// </summary>
    /// <param name="id">Kullanıcı ID'si</param>
    /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 10)</param>
    /// <returns>200 OK - Post geçmişi</returns>
    [HttpGet("{id}/posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserPostHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPosts(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Sayfalama parametrelerini doğrula
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Maksimum 50 öğe

        var result = await _userService.GetUserPostsAsync(id, pageNumber, pageSize, HttpContext.RequestAborted);

        return Ok(result);
    }
}
