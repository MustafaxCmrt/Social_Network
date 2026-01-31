using Application.DTOs.Club;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Kulüp yönetimi için API controller
/// Kulüp CRUD, başvuru ve üyelik işlemlerini yönetir
/// </summary>
public class ClubController : AppController
{
    private readonly IClubService _clubService;
    private readonly IFileService _fileService;
    private readonly IValidator<CreateClubRequestDto> _createClubRequestValidator;
    private readonly IValidator<ReviewClubRequestDto> _reviewClubRequestValidator;
    private readonly IValidator<UpdateClubDto> _updateClubValidator;
    private readonly IValidator<JoinClubDto> _joinClubValidator;
    private readonly IValidator<ProcessMembershipDto> _processMembershipValidator;
    private readonly IValidator<UpdateMemberRoleDto> _updateMemberRoleValidator;
    private readonly IValidator<KickMemberDto> _kickMemberValidator;
    private readonly ILogger<ClubController> _logger;

    public ClubController(
        IClubService clubService,
        IFileService fileService,
        IValidator<CreateClubRequestDto> createClubRequestValidator,
        IValidator<ReviewClubRequestDto> reviewClubRequestValidator,
        IValidator<UpdateClubDto> updateClubValidator,
        IValidator<JoinClubDto> joinClubValidator,
        IValidator<ProcessMembershipDto> processMembershipValidator,
        IValidator<UpdateMemberRoleDto> updateMemberRoleValidator,
        IValidator<KickMemberDto> kickMemberValidator,
        ILogger<ClubController> logger)
    {
        _clubService = clubService;
        _fileService = fileService;
        _createClubRequestValidator = createClubRequestValidator;
        _reviewClubRequestValidator = reviewClubRequestValidator;
        _updateClubValidator = updateClubValidator;
        _joinClubValidator = joinClubValidator;
        _processMembershipValidator = processMembershipValidator;
        _updateMemberRoleValidator = updateMemberRoleValidator;
        _kickMemberValidator = kickMemberValidator;
        _logger = logger;
    }

    // ==================== KULÜP BAŞVURU ENDPOİNTLERİ ====================

    /// <summary>
    /// Yeni kulüp açma başvurusu oluşturur
    /// </summary>
    [HttpPost("request")]
    [Authorize]
    [ProducesResponseType(typeof(ClubRequestListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateClubRequest([FromBody] CreateClubRequestDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createClubRequestValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.CreateClubRequestAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetMyClubRequests), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bekleyen kulüp başvurularını getirir (Moderatör/Admin)
    /// </summary>
    [HttpGet("requests/pending")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingClubRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetPendingClubRequestsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Kullanıcının kendi kulüp başvurularını getirir
    /// </summary>
    [HttpGet("requests/my")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyClubRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetMyClubRequestsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kulüp başvurusunu inceler (onay/red) - Moderatör/Admin
    /// </summary>
    [HttpPost("requests/review")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ClubRequestListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewClubRequest([FromBody] ReviewClubRequestDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _reviewClubRequestValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.ReviewClubRequestAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ==================== KULÜP CRUD ENDPOİNTLERİ ====================

    /// <summary>
    /// Tüm kulüpleri listeler (sayfalı, arama destekli)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllClubs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _clubService.GetAllClubsAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kulüp detayını ID ile getirir
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClubById(int id, CancellationToken cancellationToken)
    {
        var club = await _clubService.GetClubByIdAsync(id, cancellationToken);
        if (club == null)
            return NotFound(new { message = "Kulüp bulunamadı" });

        return Ok(club);
    }

    /// <summary>
    /// Kulüp detayını slug ile getirir
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClubBySlug(string slug, CancellationToken cancellationToken)
    {
        var club = await _clubService.GetClubBySlugAsync(slug, cancellationToken);
        if (club == null)
            return NotFound(new { message = "Kulüp bulunamadı" });

        return Ok(club);
    }

    /// <summary>
    /// Kulüp bilgilerini günceller (Başkan veya Admin)
    /// </summary>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClub([FromBody] UpdateClubDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateClubValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.UpdateClubAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Kulübü siler (Sadece Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClub(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clubService.DeleteClubAsync(id, cancellationToken);
            if (!result)
                return NotFound(new { message = "Kulüp bulunamadı" });

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Kulüp logosu yükler
    /// </summary>
    [HttpPost("{id}/upload-logo")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadClubLogo(int id, IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya yüklenemedi" });

            if (!_fileService.IsValidImageExtension(file.FileName))
                return BadRequest(new { message = "Sadece .jpg, .jpeg, .png, .gif uzantılı dosyalar yüklenebilir" });

            if (!_fileService.IsValidFileSize(file.Length))
                return BadRequest(new { message = "Dosya boyutu maksimum 5 MB olabilir" });

            var imageUrl = await _fileService.UploadImageAsync(file, "clubs/logos", cancellationToken);
            var result = await _clubService.UploadClubLogoAsync(id, imageUrl, cancellationToken);

            return Ok(new { logoUrl = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Kulüp banner'ı yükler
    /// </summary>
    [HttpPost("{id}/upload-banner")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadClubBanner(int id, IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya yüklenemedi" });

            if (!_fileService.IsValidImageExtension(file.FileName))
                return BadRequest(new { message = "Sadece .jpg, .jpeg, .png, .gif uzantılı dosyalar yüklenebilir" });

            if (!_fileService.IsValidFileSize(file.Length))
                return BadRequest(new { message = "Dosya boyutu maksimum 5 MB olabilir" });

            var imageUrl = await _fileService.UploadImageAsync(file, "clubs/banners", cancellationToken);
            var result = await _clubService.UploadClubBannerAsync(id, imageUrl, cancellationToken);

            return Ok(new { bannerUrl = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ==================== ÜYELİK ENDPOİNTLERİ ====================

    /// <summary>
    /// Kulübe katılma başvurusu yapar
    /// </summary>
    [HttpPost("join")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinClub([FromBody] JoinClubDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _joinClubValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.JoinClubAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kulüpten ayrılır
    /// </summary>
    [HttpPost("{clubId}/leave")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveClub(int clubId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clubService.LeaveClubAsync(clubId, cancellationToken);
            return Ok(new { message = "Kulüpten başarıyla ayrıldınız" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kulübün üyelerini listeler
    /// </summary>
    [HttpGet("{clubId}/members")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClubMembers(
        int clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _clubService.GetClubMembersAsync(clubId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bekleyen üyelik başvurularını getirir (Yöneticiler için)
    /// </summary>
    [HttpGet("{clubId}/members/pending")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMembers(
        int clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetPendingMembersAsync(clubId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Üyelik başvurusunu işler (onay/red)
    /// </summary>
    [HttpPost("members/process")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessMembership([FromBody] ProcessMembershipDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _processMembershipValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.ProcessMembershipAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Üye rolünü değiştirir (Sadece Başkan)
    /// </summary>
    [HttpPut("members/role")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMemberRole([FromBody] UpdateMemberRoleDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateMemberRoleValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.UpdateMemberRoleAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Üyeyi kulüpten çıkarır
    /// </summary>
    [HttpPost("members/kick")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> KickMember([FromBody] KickMemberDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _kickMemberValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation hatası",
                Errors = validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            });
        }

        try
        {
            var result = await _clubService.KickMemberAsync(dto, cancellationToken);
            return Ok(new { message = "Üye başarıyla çıkarıldı" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Başkanlığı devreder (Sadece Başkan)
    /// </summary>
    [HttpPost("{clubId}/transfer-presidency/{newPresidentUserId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferPresidency(int clubId, int newPresidentUserId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clubService.TransferPresidencyAsync(clubId, newPresidentUserId, cancellationToken);
            return Ok(new { message = "Başkanlık başarıyla devredildi" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Kullanıcının belirli bir kulüpteki üyelik durumunu getirir
    /// </summary>
    [HttpGet("{clubId}/membership-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MembershipStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembershipStatus(int clubId, CancellationToken cancellationToken)
    {
        var result = await _clubService.GetMembershipStatusAsync(clubId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının üye olduğu kulüpleri getirir
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<MyClubDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyClubs(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clubService.GetMyClubsAsync(cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
