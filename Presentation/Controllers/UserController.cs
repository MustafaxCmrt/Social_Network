using Application.DTOs.User;
using Application.Services.Abstractions;
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
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;

    public UserController(
        IUserService userService,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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

        // 2. Kullanıcı oluştur
        var result = await _userService.CreateUserAsync(request, HttpContext.RequestAborted);

        // 3. Username veya Email zaten kullanılıyor
        if (result == null)
        {
            return Conflict(new
            {
                Message = "Kullanıcı adı veya email adresi zaten kullanılıyor"
            });
        }

        // 4. Başarılı - 201 Created
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
    /// Tüm kullanıcıları listeler (Sadece Admin)
    /// </summary>
    /// <returns>
    /// 200 OK - Kullanıcı listesi
    /// 403 Forbidden - Yetki yok
    /// </returns>
    [HttpGet("getAll")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<UserListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync(HttpContext.RequestAborted);
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
        var result = await _userService.DeleteUserAsync(id, HttpContext.RequestAborted);

        if (!result)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(new { message = "Kullanıcı başarıyla silindi" });
    }
}
