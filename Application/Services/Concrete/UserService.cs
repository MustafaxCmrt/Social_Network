using Application.DTOs.User;
using Application.DTOs.AuditLog;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Kullanıcı yönetimi işlemlerini gerçekleştiren servis
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public UserService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Yeni kullanıcı oluşturur (Admin)
    /// 1. Username ve Email unique kontrolü
    /// 2. Şifreyi BCrypt ile hashle
    /// 3. Yeni kullanıcı oluştur
    /// 4. Veritabanına kaydet
    /// </summary>
    public async Task<CreateUserResponseDto?> CreateUserAsync(CreateUserDto request, CancellationToken cancellationToken = default)
    {
        // 1. Username zaten kullanılıyor mu kontrol et
        var usernameExists = await _unitOfWork.Users.AnyAsync(
            u => u.Username == request.Username && !u.IsDeleted,
            cancellationToken
        );

        if (usernameExists)
            return null; // Username zaten kullanılıyor

        // 2. Email zaten kullanılıyor mu kontrol et
        var emailExists = await _unitOfWork.Users.AnyAsync(
            u => u.Email == request.Email && !u.IsDeleted,
            cancellationToken
        );

        if (emailExists)
            return null; // Email zaten kullanılıyor

        // 3. Şifreyi BCrypt ile hashle
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 4. Role enum'a çevir
        if (!Enum.TryParse<Roles>(request.Role, ignoreCase: true, out var role))
            return null; // Geçersiz rol

        // 5. Yeni kullanıcı oluştur
        var newUser = new Users
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 6. Veritabanına kaydet
        await _unitOfWork.Users.CreateAsync(newUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Response DTO oluştur ve döndür
        return new CreateUserResponseDto
        {
            UserId = newUser.Id,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            Username = newUser.Username,
            Email = newUser.Email,
            ProfileImg = newUser.ProfileImg,
            Role = newUser.Role.ToString(),
            IsActive = newUser.IsActive,
            CreatedAt = newUser.CreatedAt
        };
    }

    /// <summary>
    /// ID'ye göre kullanıcı bilgilerini getirir
    /// </summary>
    public async Task<GetUserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            cancellationToken
        );

        if (user == null)
            return null;

        return new GetUserDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            ProfileImg = user.ProfileImg,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Admin) - Pagination ve search destekli
    /// </summary>
    public async Task<Application.DTOs.Common.PagedResultDto<UserListDto>> GetAllUsersAsync(
        int page = 1, 
        int pageSize = 10, 
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        // Parametreleri validate et
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Maksimum 100 kayıt

        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        
        // IsDeleted olmayan kullanıcıları filtrele
        var query = allUsers.Where(u => !u.IsDeleted).AsQueryable();

        // Search filtresi varsa uygula
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(lowerSearchTerm) ||
                u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                u.LastName.ToLower().Contains(lowerSearchTerm) ||
                u.Email.ToLower().Contains(lowerSearchTerm));
        }

        // Toplam kayıt sayısı
        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Pagination uygula
        var users = query
            .OrderByDescending(u => u.CreatedAt) // En yeni kullanıcılar önce
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListDto
            {
                UserId = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfileImg = u.ProfileImg,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive
            })
            .ToList();

        return new Application.DTOs.Common.PagedResultDto<UserListDto>
        {
            Items = users,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Kullanıcı bilgilerini günceller
    /// Normal kullanıcı sadece kendi profilini güncelleyebilir
    /// Admin herkesin profilini güncelleyebilir
    /// </summary>
    public async Task<UpdateUserResponseDto?> UpdateUserAsync(int userId, UpdateUserDto request, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        // 1. Kullanıcıyı bul
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            return null;

        // 2. Yetki kontrolü - Kullanıcı sadece kendi profilini güncelleyebilir (Admin hariç)
        if (!isAdmin && currentUserId != userId)
            return null;

        // 3. Username değiştiriliyorsa unique kontrolü
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _unitOfWork.Users.AnyAsync(
                u => u.Username == request.Username && u.Id != userId && !u.IsDeleted,
                cancellationToken
            );

            if (usernameExists)
                return null; // Username zaten kullanılıyor

            user.Username = request.Username;
        }

        // 4. Email değiştiriliyorsa unique kontrolü
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var emailExists = await _unitOfWork.Users.AnyAsync(
                u => u.Email == request.Email && u.Id != userId && !u.IsDeleted,
                cancellationToken
            );

            if (emailExists)
                return null; // Email zaten kullanılıyor

            user.Email = request.Email;
        }

        // 5. Temel bilgileri güncelle
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        // 6. Şifre değiştiriliyorsa hashle
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        // 7. IsActive sadece admin değiştirebilir
        if (isAdmin && request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        // 7.5. Role sadece admin değiştirebilir
        if (isAdmin && !string.IsNullOrWhiteSpace(request.Role))
        {
            if (Enum.TryParse<Roles>(request.Role, ignoreCase: true, out var role))
            {
                user.Role = role;
            }
        }

        // 8. UpdatedAt güncelle
        user.UpdatedAt = DateTime.UtcNow;

        // 9. Veritabanına kaydet
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Response DTO oluştur ve döndür
        return new UpdateUserResponseDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            ProfileImg = user.ProfileImg,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Kullanıcıyı siler (soft delete - Admin)
    /// IsDeleted flag'ini true yapar
    /// </summary>
    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            return false;

        // Soft delete
        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Audit log kaydet
        await _auditLogService.CreateLogAsync(new CreateAuditLogDto
        {
            Action = "DeleteUser",
            EntityType = "User",
            EntityId = userId,
            OldValue = $"Username: {user.Username}, Email: {user.Email}, Role: {user.Role}",
            NewValue = "Deleted (soft delete)",
            Success = true
        }, cancellationToken);

        return true;
    }

    /// <summary>
    /// Giriş yapan kullanıcının kendi profil bilgilerini getirir
    /// </summary>
    public async Task<GetUserDto?> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Public kullanıcı profili getirir (thread/post sayılarıyla)
    /// </summary>
    public async Task<UserProfileDto?> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            cancellationToken
        );

        if (user == null)
            return null;

        // Thread ve Post sayılarını al
        var totalThreads = await _unitOfWork.Threads.CountAsync(
            t => t.UserId == userId && !t.IsDeleted,
            cancellationToken
        );

        var totalPosts = await _unitOfWork.Posts.CountAsync(
            p => p.UserId == userId && !p.IsDeleted,
            cancellationToken
        );

        return new UserProfileDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            ProfileImg = user.ProfileImg,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            TotalThreads = totalThreads,
            TotalPosts = totalPosts
        };
    }

    /// <summary>
    /// Giriş yapan kullanıcının kendi profilini günceller
    /// Şifre değişikliği için mevcut şifre doğrulaması yapar
    /// </summary>
    public async Task<GetUserDto?> UpdateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            return null;

        // 1. Username değiştiriliyorsa unique kontrolü
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _unitOfWork.Users.AnyAsync(
                u => u.Username == request.Username && u.Id != userId && !u.IsDeleted,
                cancellationToken
            );

            if (usernameExists)
                throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor");

            user.Username = request.Username;
        }

        // 2. Email değiştiriliyorsa unique kontrolü
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var emailExists = await _unitOfWork.Users.AnyAsync(
                u => u.Email == request.Email && u.Id != userId && !u.IsDeleted,
                cancellationToken
            );

            if (emailExists)
                throw new InvalidOperationException("Bu email adresi zaten kullanılıyor");

            user.Email = request.Email;
        }

        // 3. Temel bilgileri güncelle
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.ProfileImg))
        {
            user.ProfileImg = request.ProfileImg;
        }

        // 4. Şifre değişikliği
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            // Mevcut şifre kontrolü
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                throw new UnauthorizedAccessException("Şifre değiştirmek için mevcut şifrenizi girmelisiniz");
            }

            // Mevcut şifre doğrulama
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Mevcut şifre hatalı");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            
            // Şifre değiştiğinde refresh token versiyonunu artır (güvenlik)
            user.RefreshTokenVersion++;
        }

        // 5. UpdatedAt güncelle
        user.UpdatedAt = DateTime.UtcNow;

        // 6. Veritabanına kaydet
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Kullanıcının kendi hesabını siler
    /// Email ve Username'e suffix ekleyerek aynı bilgilerle yeni hesap açılabilmesini sağlar
    /// </summary>
    public async Task<bool> DeleteMyAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            return false;

        // Email ve Username'e timestamp suffix ekle (unique constraint bypass)
        var timestamp = DateTime.UtcNow.Ticks;
        user.Email = $"{user.Email}_DELETED_{timestamp}";
        user.Username = $"{user.Username}_DELETED_{timestamp}";

        // Soft delete
        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;
        user.IsActive = false;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Kullanıcının oluşturduğu thread'leri getirir (sayfalama ile)
    /// </summary>
    public async Task<UserThreadHistoryDto> GetUserThreadsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            cancellationToken
        );

        if (user == null)
        {
            return new UserThreadHistoryDto
            {
                UserId = userId,
                Username = string.Empty,
                TotalThreads = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                Threads = new List<Application.DTOs.Thread.ThreadDto>()
            };
        }

        // Toplam thread sayısını al
        var totalThreads = await _unitOfWork.Threads.CountAsync(
            t => t.UserId == userId && !t.IsDeleted,
            cancellationToken
        );

        // Toplam sayfa sayısını hesapla
        var totalPages = (int)Math.Ceiling(totalThreads / (double)pageSize);

        // Thread'leri Posts bilgisiyle birlikte getir (PostCount için)
        var allThreads = await _unitOfWork.Threads.GetAllWithIncludesAsync(
            include: q => q.Include(t => t.Posts),
            cancellationToken
        );
        var userThreads = allThreads
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new Application.DTOs.Thread.ThreadDto
            {
                Id = t.Id,
                Title = t.Title,
                Content = t.Content,
                ViewCount = t.ViewCount,
                PostCount = t.Posts.Count(p => !p.IsDeleted),
                IsSolved = t.IsSolved,
                UserId = t.UserId,
                CategoryId = t.CategoryId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToList();

        return new UserThreadHistoryDto
        {
            UserId = userId,
            Username = user.Username,
            TotalThreads = totalThreads,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Threads = userThreads
        };
    }

    /// <summary>
    /// Kullanıcının yazdığı post'ları getirir (sayfalama ile)
    /// </summary>
    public async Task<UserPostHistoryDto> GetUserPostsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            cancellationToken
        );

        if (user == null)
        {
            return new UserPostHistoryDto
            {
                UserId = userId,
                Username = string.Empty,
                TotalPosts = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0,
                Posts = new List<Application.DTOs.Post.PostDto>()
            };
        }

        // Toplam post sayısını al
        var totalPosts = await _unitOfWork.Posts.CountAsync(
            p => p.UserId == userId && !p.IsDeleted,
            cancellationToken
        );

        // Toplam sayfa sayısını hesapla
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

        // Post'ları Thread bilgisiyle birlikte getir
        var allPosts = await _unitOfWork.Posts.GetAllWithIncludesAsync(
            include: q => q.Include(p => p.Thread),
            cancellationToken
        );
        var userPosts = allPosts
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new Application.DTOs.Post.PostDto
            {
                Id = p.Id,
                ThreadId = p.ThreadId,
                ThreadTitle = p.Thread?.Title ?? "Bilinmeyen Konu",
                UserId = p.UserId,
                Content = p.Content,
                Img = p.Img,
                IsSolution = p.IsSolution,
                UpvoteCount = p.UpvoteCount,
                ParentPostId = p.ParentPostId,
                ReplyCount = p.Replies.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToList();

        return new UserPostHistoryDto
        {
            UserId = userId,
            Username = user.Username,
            TotalPosts = totalPosts,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Posts = userPosts
        };
    }

    /// <summary>
    /// Kullanıcının profil resmini günceller
    /// </summary>
    public async Task<string?> UpdateProfileImageAsync(int userId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            return null;

        user.ProfileImg = imageUrl;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ProfileImg;
    }
}
