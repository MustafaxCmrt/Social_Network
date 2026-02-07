using Application.DTOs.Club;
using Application.DTOs.Common;
using Application.Services.Abstractions;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

/// <summary>
/// Kulüp yönetimi için API controller
/// </summary>
public class ClubController : AppController
{
    private readonly IClubService _clubService;
    private readonly IFileService _fileService;
    private readonly IValidator<CreateClubDto> _createClubValidator;
    private readonly IValidator<CreateClubRequestDto> _createClubRequestValidator;
    private readonly IValidator<ReviewClubRequestDto> _reviewClubRequestValidator;
    private readonly IValidator<UpdateClubDto> _updateClubValidator;
    private readonly IValidator<JoinClubDto> _joinClubValidator;
    private readonly IValidator<ProcessMembershipDto> _processMembershipValidator;
    private readonly IValidator<UpdateMemberRoleDto> _updateMemberRoleValidator;
    private readonly IValidator<UpdateClubApplicationStatusDto> _updateApplicationStatusValidator;

    public ClubController(
        IClubService clubService,
        IFileService fileService,
        IValidator<CreateClubDto> createClubValidator,
        IValidator<CreateClubRequestDto> createClubRequestValidator,
        IValidator<ReviewClubRequestDto> reviewClubRequestValidator,
        IValidator<UpdateClubDto> updateClubValidator,
        IValidator<JoinClubDto> joinClubValidator,
        IValidator<ProcessMembershipDto> processMembershipValidator,
        IValidator<UpdateMemberRoleDto> updateMemberRoleValidator,
        IValidator<UpdateClubApplicationStatusDto> updateApplicationStatusValidator)
    {
        _clubService = clubService;
        _fileService = fileService;
        _createClubValidator = createClubValidator;
        _createClubRequestValidator = createClubRequestValidator;
        _reviewClubRequestValidator = reviewClubRequestValidator;
        _updateClubValidator = updateClubValidator;
        _joinClubValidator = joinClubValidator;
        _processMembershipValidator = processMembershipValidator;
        _updateMemberRoleValidator = updateMemberRoleValidator;
        _updateApplicationStatusValidator = updateApplicationStatusValidator;
    }
    
    /// <summary>
    /// Yeni kulüp açma başvurusu oluşturur
    /// </summary>
    [HttpPost("requests/create")]
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
            return CreatedAtAction(nameof(GetPendingClubRequests), new { page = 1, pageSize = 10 }, result);
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
    [HttpGet("requests/get-pending")]
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
    /// Kulüp başvurusunu inceler (onay/red) - Moderatör/Admin
    /// </summary>
    [HttpPut("requests/{requestId}/review")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ClubRequestListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewClubRequest(int requestId, [FromBody] ReviewClubRequestDto dto, CancellationToken cancellationToken)
    {
        // URL'deki requestId ile DTO'daki eşleşmeli
        if (requestId != dto.RequestId)
            return BadRequest(new { message = "URL ve body'deki request ID eşleşmiyor" });

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
    /// Yeni kulüp oluşturur (Sadece Admin - başvuru olmadan doğrudan oluşturma)
    /// </summary>
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateClub([FromBody] CreateClubDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createClubValidator.ValidateAsync(dto, cancellationToken);
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
            var result = await _clubService.CreateClubDirectAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetClub), new { identifier = result.Id }, result);
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
    /// Tüm kulüpleri listeler (sayfalı, arama destekli)
    /// </summary>
    [HttpGet("get-all")]
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
    /// Kulüp detayını getirir (ID veya slug ile)
    /// </summary>
    [HttpGet("get/{identifier}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClub(string identifier, CancellationToken cancellationToken)
    {
        var club = await _clubService.GetClubByIdentifierAsync(identifier, cancellationToken);
        if (club == null)
            return NotFound(new { message = "Kulüp bulunamadı" });

        return Ok(club);
    }

    /// <summary>
    /// Kulüp bilgilerini günceller (Başkan veya Admin)
    /// </summary>
    [HttpPut("update")]
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
    [HttpDelete("delete/{id}")]
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
    /// Kulüp resmi yükler (logo veya banner)
    /// </summary>
    [HttpPost("{id}/upload-image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadClubImage(int id, IFormFile file, [FromQuery] string type, CancellationToken cancellationToken)
    {
        try
        {
            if (type != "logo" && type != "banner")
                return BadRequest(new { message = "Geçersiz resim tipi. 'logo' veya 'banner' olmalıdır" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya yüklenemedi" });

            if (!_fileService.IsValidImageExtension(file.FileName))
                return BadRequest(new { message = "Sadece .jpg, .jpeg, .png, .gif uzantılı dosyalar yüklenebilir" });

            if (!_fileService.IsValidFileSize(file.Length))
                return BadRequest(new { message = "Dosya boyutu maksimum 5 MB olabilir" });

            var folder = type == "logo" ? "clubs/logos" : "clubs/banners";
            var imageUrl = await _fileService.UploadImageAsync(file, folder, cancellationToken);
            var result = await _clubService.UploadClubImageAsync(id, imageUrl, type, cancellationToken);

            return Ok(new { imageUrl = result });
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
    /// Kulübe katılma başvurusu yapar
    /// </summary>
    [HttpPost("{clubId}/join")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinClub(int clubId, [FromBody] JoinClubDto dto, CancellationToken cancellationToken)
    {
        // URL'deki clubId ile DTO'daki eşleşmeli
        if (clubId != dto.ClubId)
            return BadRequest(new { message = "URL ve body'deki club ID eşleşmiyor" });

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
            await _clubService.LeaveClubAsync(clubId, cancellationToken);
            return Ok(new { message = "Kulüpten başarıyla ayrıldınız" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kulübün üyelerini listeler (status ile filtrelenebilir: Approved, Pending)
    /// </summary>
    [HttpGet("{clubId}/members")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClubMembers(
        int clubId,
        [FromQuery] MembershipStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetClubMembersAsync(clubId, page, pageSize, status, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Üyelik başvurusunu işler (onay/red/çıkarma)
    /// </summary>
    [HttpPut("memberships/{membershipId}")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessMembership(int membershipId, [FromBody] ProcessMembershipDto dto, CancellationToken cancellationToken)
    {
        // URL'deki membershipId ile DTO'daki eşleşmeli
        if (membershipId != dto.MembershipId)
            return BadRequest(new { message = "URL ve body'deki membership ID eşleşmiyor" });

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
    /// Üye rolünü değiştirir veya başkanlığı devreder (Sadece Başkan)
    /// </summary>
    [HttpPut("memberships/{membershipId}/role")]
    [Authorize]
    [ProducesResponseType(typeof(ClubMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMemberRole(int membershipId, [FromBody] UpdateMemberRoleDto dto, CancellationToken cancellationToken)
    {
        // URL'deki membershipId ile DTO'daki eşleşmeli
        if (membershipId != dto.MembershipId)
            return BadRequest(new { message = "URL ve body'deki membership ID eşleşmiyor" });

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
    /// Kullanıcının üye olduğu kulüpleri getirir
    /// </summary>
    [HttpGet("get-mine")]
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

    /// <summary>
    /// Kullanıcının kulüp başvurularını getirir
    /// </summary>
    [HttpGet("my-applications")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ClubApplicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetUserClubApplicationsAsync(page, pageSize, status, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kulüp başvuru durumunu günceller (Admin/Moderator)
    /// </summary>
    [HttpPatch("{id}/application-status")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplicationStatus(
        int id,
        [FromBody] UpdateClubApplicationStatusDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateApplicationStatusValidator.ValidateAsync(dto, cancellationToken);
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
            var result = await _clubService.UpdateClubApplicationStatusAsync(id, dto, cancellationToken);
            if (!result)
                return NotFound(new { message = "Kulüp bulunamadı" });

            return Ok(new { message = "Başvuru durumu güncellendi" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bekleyen üyelik başvurularını getirir (Kulüp yöneticileri için)
    /// </summary>
    /// <remarks>
    /// - Admin/Moderator: Tüm bekleyen başvuruları görebilir
    /// - Kulüp yöneticileri: Sadece kendi kulüplerinin bekleyen başvurularını görebilir
    /// </remarks>
    [HttpGet("memberships/pending")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<ClubMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingMemberships(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubService.GetPendingMembershipsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
