using Application.DTOs.Post;
using Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Abstraction;

namespace Presentation.Controllers;

public class PostController : AppController
{
    private readonly IPostService _postService;
    private readonly IFileService _fileService;
    private readonly ILogger<PostController> _logger;
    private readonly IValidator<CreatePostDto> _createValidator;
    private readonly IValidator<UpdatePostDto> _updateValidator;
    private readonly IValidator<MarkSolutionDto> _markSolutionValidator;

    public PostController(
        IPostService postService,
        IFileService fileService,
        ILogger<PostController> logger,
        IValidator<CreatePostDto> createValidator,
        IValidator<UpdatePostDto> updateValidator,
        IValidator<MarkSolutionDto> markSolutionValidator)
    {
        _postService = postService;
        _fileService = fileService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _markSolutionValidator = markSolutionValidator;
    }

    [HttpGet("getAll/{threadId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPostsByThreadId(
        int threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var posts = await _postService.GetAllPostsByThreadIdAsync(threadId, page, pageSize, cancellationToken);
        return Ok(posts);
    }

    [HttpGet("get/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(int id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetPostByIdAsync(id, cancellationToken);

        if (post == null)
        {
            return NotFound(new { message = $"ID: {id} olan yorum bulunamadı." });
        }

        return Ok(post);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto createPostDto, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(createPostDto, cancellationToken);
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

        try
        {
            var post = await _postService.CreatePostAsync(createPostDto, cancellationToken);
            return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post create unauthorized");
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> UpdatePost([FromBody] UpdatePostDto updatePostDto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(updatePostDto, cancellationToken);
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

        try
        {
            var post = await _postService.UpdatePostAsync(updatePostDto, cancellationToken);
            return Ok(post);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post update forbidden");
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _postService.DeletePostAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"ID: {id} olan yorum bulunamadı." });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Post delete forbidden");
            return Forbid();
        }
    }

    [HttpPost("markSolution")]
    [Authorize]
    public async Task<IActionResult> MarkSolution([FromBody] MarkSolutionDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _markSolutionValidator.ValidateAsync(request, cancellationToken);
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

        try
        {
            var result = await _postService.MarkSolutionAsync(request, cancellationToken);
            return Ok(new { success = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "MarkSolution forbidden");
            return Forbid();
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
    /// Post için resim yükler
    /// </summary>
    /// <param name="file">Yüklenecek resim dosyası</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Resim başarıyla yüklendi, URL döner
    /// 400 Bad Request - Geçersiz dosya (boyut/uzantı)
    /// 401 Unauthorized - Kullanıcı giriş yapmamış
    /// </returns>
    [HttpPost("upload-image")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
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
            var imageUrl = await _fileService.UploadImageAsync(file, "posts", cancellationToken);

            _logger.LogInformation("Post resmi yüklendi: {ImageUrl}", imageUrl);

            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resim yükleme hatası");
            return BadRequest(new { message = "Resim yüklenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Post'a upvote (beğeni) verir
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Upvote verildi
    /// 400 Bad Request - Zaten beğenilmiş
    /// 401 Unauthorized - Giriş yapılmamış
    /// 404 Not Found - Post bulunamadı
    /// </returns>
    [HttpPost("{id}/upvote")]
    [Authorize]
    [ProducesResponseType(typeof(UpvoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpvotePost(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut kullanıcı ID'sini al
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var result = await _postService.UpvotePostAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upvote hatası - PostId: {PostId}", id);
            return BadRequest(new { message = "Upvote işlemi başarısız" });
        }
    }

    /// <summary>
    /// Post'tan upvote'u geri alır
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Upvote geri alındı
    /// 400 Bad Request - Zaten beğenilmemiş
    /// 401 Unauthorized - Giriş yapılmamış
    /// 404 Not Found - Post bulunamadı
    /// </returns>
    [HttpDelete("{id}/upvote")]
    [Authorize]
    [ProducesResponseType(typeof(UpvoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUpvote(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut kullanıcı ID'sini al
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var result = await _postService.RemoveUpvoteAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove upvote hatası - PostId: {PostId}", id);
            return BadRequest(new { message = "Upvote geri alma işlemi başarısız" });
        }
    }

    /// <summary>
    /// Kullanıcının post'a verdiği upvote durumunu getirir
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Upvote durumu
    /// 401 Unauthorized - Giriş yapılmamış
    /// 404 Not Found - Post bulunamadı
    /// </returns>
    [HttpGet("{id}/vote-status")]
    [Authorize]
    [ProducesResponseType(typeof(VoteStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoteStatus(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut kullanıcı ID'sini al
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var result = await _postService.GetVoteStatusAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vote status hatası - PostId: {PostId}", id);
            return BadRequest(new { message = "Upvote durumu alınamadı" });
        }
    }
}
