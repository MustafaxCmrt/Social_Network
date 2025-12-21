using Application.DTOs.User;

namespace Application.Services.Abstractions;

/// <summary>
/// Kullanıcı yönetimi işlemlerini yöneten interface
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Yeni kullanıcı oluşturur (Admin)
    /// </summary>
    /// <param name="request">Oluşturulacak kullanıcı bilgileri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Oluşturulan kullanıcı bilgileri, başarısızsa null</returns>
    Task<CreateUserResponseDto?> CreateUserAsync(CreateUserDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre kullanıcı bilgilerini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kullanıcı bilgileri, bulunamazsa null</returns>
    Task<GetUserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm kullanıcıları listeler (Admin)
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kullanıcı listesi</returns>
    Task<List<UserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı bilgilerini günceller
    /// </summary>
    /// <param name="userId">Güncellenecek kullanıcının ID'si</param>
    /// <param name="request">Güncellenmiş kullanıcı bilgileri</param>
    /// <param name="currentUserId">İşlemi yapan kullanıcının ID'si</param>
    /// <param name="isAdmin">İşlemi yapan kullanıcı admin mi?</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Güncellenmiş kullanıcı bilgileri, başarısızsa null</returns>
    Task<UpdateUserResponseDto?> UpdateUserAsync(int userId, UpdateUserDto request, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıyı siler (soft delete - Admin)
    /// </summary>
    /// <param name="userId">Silinecek kullanıcının ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silme başarılıysa true, değilse false</returns>
    Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
}
