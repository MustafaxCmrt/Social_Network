using Application.DTOs.User;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Persistence.UnitOfWork;

namespace Application.Services.Concrete;

/// <summary>
/// Kullanıcı yönetimi işlemlerini gerçekleştiren servis
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        if (!Enum.TryParse<Roles>(request.Role, out var role))
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
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Admin)
    /// </summary>
    public async Task<List<UserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        
        // IsDeleted olmayan kullanıcıları filtrele
        var users = allUsers.Where(u => !u.IsDeleted).ToList();

        return users.Select(u => new UserListDto
        {
            UserId = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.ToString(),
            IsActive = u.IsActive
        }).ToList();
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
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
