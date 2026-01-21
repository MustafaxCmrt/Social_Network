using Application.DTOs.Post;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Application.Common.Extensions;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

public class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public PostService(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public async Task<PagedResultDto<PostDto>> GetAllPostsByThreadIdAsync(
        int threadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Sadece ana yorumları getir (ParentPostId = null)
        var posts = await _unitOfWork.Posts.FindAsync(
            p => p.ThreadId == threadId && p.ParentPostId == null, 
            cancellationToken);

        // Sırala ve sayfalandır
        var ordered = posts
            .OrderByDescending(p => p.IsSolution)
            .ThenByDescending(p => p.UpvoteCount) // En çok beğenileni üste
            .ThenByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id);

        // Extension metod ile sayfalandır
        return ordered.ToPagedResult(page, pageSize, MapToDto);
    }

    public async Task<PostDto?> GetPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(id, cancellationToken);
        return post == null ? null : MapToDto(post);
    }

    public async Task<PostDto> CreatePostAsync(CreatePostDto createPostDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var thread = await _unitOfWork.Threads.GetByIdAsync(createPostDto.ThreadId, cancellationToken);
        if (thread == null)
        {
            throw new KeyNotFoundException($"Konu ID: {createPostDto.ThreadId} bulunamadı.");
        }

        // ParentPostId varsa, ana yorumun varlığını kontrol et
        if (createPostDto.ParentPostId.HasValue)
        {
            var parentPost = await _unitOfWork.Posts.GetByIdAsync(createPostDto.ParentPostId.Value, cancellationToken);
            if (parentPost == null)
            {
                throw new KeyNotFoundException($"Cevap verilecek yorum ID: {createPostDto.ParentPostId} bulunamadı.");
            }

            // ParentPost'un ThreadId ile uyumlu olduğunu kontrol et
            if (parentPost.ThreadId != createPostDto.ThreadId)
            {
                throw new InvalidOperationException("Cevap verilecek yorum farklı bir konuya ait.");
            }

            // Sadece 1 seviye cevap izni (ana yoruma cevap, cevaba cevap yok)
            if (parentPost.ParentPostId.HasValue)
            {
                throw new InvalidOperationException("Cevaba cevap verilemez. Sadece ana yorumlara cevap verebilirsiniz.");
            }
        }

        var post = new Posts
        {
            ThreadId = createPostDto.ThreadId,
            UserId = currentUserId.Value,
            Content = createPostDto.Content,
            Img = createPostDto.Img,
            ParentPostId = createPostDto.ParentPostId,
            IsSolution = false
        };

        await _unitOfWork.Posts.CreateAsync(post, cancellationToken);
        
        // Thread'in PostCount'unu artır (ana yorum ise)
        if (!createPostDto.ParentPostId.HasValue)
        {
            thread.PostCount++;
            _unitOfWork.Threads.Update(thread);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ✨ BİLDİRİM GÖNDER
        await SendNotificationAfterPostCreatedAsync(
            post, 
            thread, 
            currentUserId.Value, 
            cancellationToken);

        return MapToDto(post);
    }

    public async Task<PostDto> UpdatePostAsync(UpdatePostDto updatePostDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var currentRole = _currentUserService.GetCurrentUserRole();
        var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var post = await _unitOfWork.Posts.GetByIdAsync(updatePostDto.Id, cancellationToken);
        if (post == null)
        {
            throw new KeyNotFoundException($"ID: {updatePostDto.Id} olan yorum bulunamadı.");
        }

        if (!isAdmin && post.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Bu yorumu güncelleme yetkiniz yok.");
        }

        post.Content = updatePostDto.Content;
        post.Img = updatePostDto.Img;

        _unitOfWork.Posts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(post);
    }

    public async Task<bool> DeletePostAsync(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var currentRole = _currentUserService.GetCurrentUserRole();
        var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var post = await _unitOfWork.Posts.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            return false;
        }

        if (!isAdmin && post.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Bu yorumu silme yetkiniz yok.");
        }

        _unitOfWork.Posts.Delete(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> MarkSolutionAsync(MarkSolutionDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("Oturum bilgisi bulunamadı.");
        }

        var currentRole = _currentUserService.GetCurrentUserRole();
        var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var thread = await _unitOfWork.Threads.GetByIdAsync(request.ThreadId, cancellationToken);
        if (thread == null)
        {
            throw new KeyNotFoundException($"Konu ID: {request.ThreadId} bulunamadı.");
        }

        if (!isAdmin && thread.UserId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Bu konu için çözüm işaretleme yetkiniz yok.");
        }

        var post = await _unitOfWork.Posts.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
        {
            throw new KeyNotFoundException($"Yorum ID: {request.PostId} bulunamadı.");
        }

        if (post.ThreadId != request.ThreadId)
        {
            throw new InvalidOperationException("Seçilen yorum bu konuya ait değil.");
        }

        // Çoklu entity update olduğu için transaction kullan
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Önce bu konu altındaki mevcut çözümü kaldır (tek çözüm kuralı)
            var existingSolutions = (await _unitOfWork.Posts.FindAsync(
                p => p.ThreadId == request.ThreadId && p.IsSolution,
                cancellationToken)).ToList();

            if (existingSolutions.Count > 0)
            {
                foreach (var solution in existingSolutions)
                {
                    solution.IsSolution = false;
                }

                _unitOfWork.Posts.UpdateRange(existingSolutions);
            }

            // Seçilen yorumu çözüm yap
            post.IsSolution = true;
            _unitOfWork.Posts.Update(post);

            // Konuyu çözüldü işaretle
            thread.IsSolved = true;
            _unitOfWork.Threads.Update(thread);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UpvoteResponseDto> UpvotePostAsync(int postId, int userId, CancellationToken cancellationToken = default)
    {
        // 1. Post var mı kontrol et
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post ID {postId} bulunamadı");
        }

        // 2. Daha önce upvote vermişmi kontrol et
        var existingVote = (await _unitOfWork.PostVotes.FindAsync(
            pv => pv.PostId == postId && pv.UserId == userId,
            cancellationToken)).FirstOrDefault();

        if (existingVote != null)
        {
            // Zaten beğenmiş
            return new UpvoteResponseDto
            {
                PostId = postId,
                IsUpvoted = true,
                TotalUpvotes = post.UpvoteCount,
                Message = "Bu yorumu zaten beğendiniz"
            };
        }

        // 3. Yeni upvote ekle
        var vote = new PostVotes
        {
            PostId = postId,
            UserId = userId
        };
        await _unitOfWork.PostVotes.CreateAsync(vote, cancellationToken);

        // 4. Post'un upvote count'unu artır
        post.UpvoteCount++;
        _unitOfWork.Posts.Update(post);

        // 5. Kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpvoteResponseDto
        {
            PostId = postId,
            IsUpvoted = true,
            TotalUpvotes = post.UpvoteCount,
            Message = "Yorum beğenildi"
        };
    }

    public async Task<UpvoteResponseDto> RemoveUpvoteAsync(int postId, int userId, CancellationToken cancellationToken = default)
    {
        // 1. Post var mı kontrol et
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post ID {postId} bulunamadı");
        }

        // 2. Upvote var mı kontrol et
        var existingVote = (await _unitOfWork.PostVotes.FindAsync(
            pv => pv.PostId == postId && pv.UserId == userId,
            cancellationToken)).FirstOrDefault();

        if (existingVote == null)
        {
            // Zaten beğenmemiş
            return new UpvoteResponseDto
            {
                PostId = postId,
                IsUpvoted = false,
                TotalUpvotes = post.UpvoteCount,
                Message = "Bu yorumu zaten beğenmemiştiniz"
            };
        }

        // 3. Upvote'u sil
        _unitOfWork.PostVotes.Delete(existingVote);

        // 4. Post'un upvote count'unu azalt
        if (post.UpvoteCount > 0)
        {
            post.UpvoteCount--;
            _unitOfWork.Posts.Update(post);
        }

        // 5. Kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpvoteResponseDto
        {
            PostId = postId,
            IsUpvoted = false,
            TotalUpvotes = post.UpvoteCount,
            Message = "Beğeni geri alındı"
        };
    }

    public async Task<VoteStatusDto> GetVoteStatusAsync(int postId, int userId, CancellationToken cancellationToken = default)
    {
        // 1. Post var mı kontrol et
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post ID {postId} bulunamadı");
        }

        // 2. Kullanıcı beğenmiş mi kontrol et
        var vote = (await _unitOfWork.PostVotes.FindAsync(
            pv => pv.PostId == postId && pv.UserId == userId,
            cancellationToken)).FirstOrDefault();

        return new VoteStatusDto
        {
            PostId = postId,
            IsUpvoted = vote != null,
            TotalUpvotes = post.UpvoteCount
        };
    }

    public async Task<PagedResultDto<PostDto>> GetPostRepliesAsync(
        int postId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        // 1. Ana yorum var mı kontrol et
        var parentPost = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (parentPost == null)
        {
            throw new KeyNotFoundException($"Post ID {postId} bulunamadı");
        }

        // 2. Bu yorumun cevaplarını getir
        var replies = await _unitOfWork.Posts.FindAsync(
            p => p.ParentPostId == postId, 
            cancellationToken);

        // 3. En çok beğenilene göre sırala ve sayfalandır
        var ordered = replies
            .OrderByDescending(p => p.UpvoteCount)
            .ThenByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id);

        // Extension metod ile sayfalandır
        return ordered.ToPagedResult(page, pageSize, MapToDto);
    }

    private PostDto MapToDto(Posts post)
    {
        // Reply count hesapla (senkron - zaten bellekteki liste üzerinden)
        var replyCount = _unitOfWork.Posts
            .FindAsync(p => p.ParentPostId == post.Id)
            .Result
            .Count();

        return new PostDto
        {
            Id = post.Id,
            ThreadId = post.ThreadId,
            UserId = post.UserId,
            Content = post.Content,
            Img = post.Img,
            IsSolution = post.IsSolution,
            UpvoteCount = post.UpvoteCount,
            ParentPostId = post.ParentPostId,
            ReplyCount = replyCount,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }

    /// <summary>
    /// Post oluşturulduktan sonra ilgili kullanıcılara bildirim gönderir
    /// </summary>
    private async Task SendNotificationAfterPostCreatedAsync(
        Posts post,
        Threads thread,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        // Kullanıcı kendi yorumuna bildirim almasın
        int? recipientUserId = null;
        NotificationType notificationType = NotificationType.ThreadReply; // Default
        string message = string.Empty; // Default

        // SENARYO 1: Yoruma cevap (ParentPostId var)
        if (post.ParentPostId.HasValue)
        {
            var parentPost = await _unitOfWork.Posts.GetByIdAsync(post.ParentPostId.Value, cancellationToken);
            if (parentPost != null)
            {
                recipientUserId = parentPost.UserId;
                notificationType = NotificationType.PostReply;
                
                // Actor kullanıcı bilgilerini al
                var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
                var actorName = actor?.Username ?? "Birileri";
                
                message = $"{actorName} yorumunuza cevap verdi";
            }
        }
        // SENARYO 2: Thread'e yorum (ana yorum)
        else
        {
            recipientUserId = thread.UserId;
            notificationType = NotificationType.ThreadReply;
            
            // Actor kullanıcı bilgilerini al
            var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
            var actorName = actor?.Username ?? "Birileri";
            
            message = $"{actorName} thread'inize cevap verdi: {thread.Title}";
        }

        // Kullanıcı kendi yorumuna bildirim almasın
        if (recipientUserId.HasValue && recipientUserId.Value != actorUserId)
        {
            await _notificationService.CreateNotificationAsync(
                new CreateNotificationDto
                {
                    UserId = recipientUserId.Value,
                    ActorUserId = actorUserId,
                    Type = notificationType,
                    Message = message,
                    ThreadId = thread.Id,
                    PostId = post.Id
                },
                cancellationToken);
        }
    }
}
