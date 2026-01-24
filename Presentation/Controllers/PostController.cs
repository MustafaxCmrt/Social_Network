using Application.DTOs.Post;
using Application.DTOs.Common;
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

    /// <summary>
    /// Bir yorumun altındaki cevapları getirir
    /// </summary>
    /// <param name="id">Ana yorumun ID'si</param>
    /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı (varsayılan: 20)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Cevaplar listesi
    /// 404 Not Found - Ana yorum bulunamadı
    /// </returns>
    [HttpGet("{id}/replies")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResultDto<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostReplies(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var replies = await _postService.GetPostRepliesAsync(id, page, pageSize, cancellationToken);
            return Ok(replies);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Yeni post oluşturur (JSON body ile)
    /// Resim eklemek için önce upload-image endpoint'ini kullanıp URL alın,
    /// sonra bu URL'i Img alanına gönderin
    /// </summary>
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

    /// <summary>
    /// Resim dosyası ile birlikte yeni post oluşturur (multipart/form-data)
    /// </summary>
    /// <param name="threadId">Konu ID</param>
    /// <param name="content">Yorum içeriği</param>
    /// <param name="parentPostId">Cevap verilecek yorum ID (opsiyonel)</param>
    /// <param name="image">Resim dosyası (opsiyonel)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    [HttpPost("create-with-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePostWithImage(
        [FromForm] int threadId,
        [FromForm] string content,
        [FromForm] int? parentPostId,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        try
        {
            string? imageUrl = null;

            // Resim varsa yükle
            if (image != null && image.Length > 0)
            {
                // Uzantı kontrolü
                if (!_fileService.IsValidImageExtension(image.FileName))
                {
                    return BadRequest(new { message = "Sadece .jpg, .jpeg, .png, .gif uzantılı dosyalar yüklenebilir" });
                }

                // Boyut kontrolü
                if (!_fileService.IsValidFileSize(image.Length))
                {
                    return BadRequest(new { message = "Dosya boyutu maksimum 5 MB olabilir" });
                }

                // Dosyayı yükle
                imageUrl = await _fileService.UploadImageAsync(image, "posts", cancellationToken);
            }

            // Post DTO oluştur
            var createPostDto = new CreatePostDto
            {
                ThreadId = threadId,
                Content = content,
                ParentPostId = parentPostId,
                Img = imageUrl
            };

            // Validasyon
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

            var post = await _postService.CreatePostAsync(createPostDto, cancellationToken);
            _logger.LogInformation("Post oluşturuldu - ID: {PostId}, Resim: {HasImage}", post.Id, imageUrl != null);
            
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Post oluşturma hatası");
            return BadRequest(new { message = "Post oluşturulurken bir hata oluştu" });
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

            return Ok(new { message = "Yorum başarıyla silindi." });
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
    /// Bir konudaki çözüm işaretini kaldırır
    /// </summary>
    /// <param name="threadId">Konu ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Çözüm başarıyla kaldırıldı
    /// 400 Bad Request - Zaten çözüm yok
    /// 401 Unauthorized - Oturum gerekli
    /// 403 Forbidden - Yetki yok
    /// 404 Not Found - Konu bulunamadı
    /// </returns>
    [HttpDelete("unmarkSolution/{threadId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnmarkSolution(int threadId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _postService.UnmarkSolutionAsync(threadId, cancellationToken);
            if (!result)
            {
                return BadRequest(new { message = "Bu konuda çözüm olarak işaretlenmiş bir yorum bulunmuyor." });
            }
            return Ok(new { message = "Çözüm işareti başarıyla kaldırıldı." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "UnmarkSolution forbidden");
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
    /// Post beğenisini toggle eder (varsa kaldir, yoksa ekle)
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>
    /// 200 OK - Beğeni toggle edildi
    /// 401 Unauthorized - Giriş yapılmamiş
    /// 404 Not Found - Post bulunamadi
    /// </returns>
    [HttpPatch("{id}/upvote")]
    [Authorize]
    [ProducesResponseType(typeof(UpvoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUpvote(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut kullanıcı ID'sini al
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var result = await _postService.ToggleUpvoteAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toggle upvote hatası - PostId: {PostId}", id);
            return BadRequest(new { message = "Beğeni işlemi başarısız" });
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
