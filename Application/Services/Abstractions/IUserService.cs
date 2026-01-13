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

    /// <summary>
    /// Giriş yapan kullanıcının kendi profil bilgilerini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Kullanıcı profil bilgileri</returns>
    Task<GetUserDto?> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Public kullanıcı profili getirir (istatistiklerle)
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Public profil bilgileri</returns>
    Task<UserProfileDto?> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Giriş yapan kullanıcının kendi profilini günceller
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="request">Güncellenmiş profil bilgileri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Güncellenmiş profil bilgileri</returns>
    Task<GetUserDto?> UpdateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının kendi hesabını siler (soft delete + email/username suffix)
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silme başarılıysa true</returns>
    Task<bool> DeleteMyAccountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının oluşturduğu thread'leri getirir (sayfalama ile)
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Thread listesi</returns>
    Task<UserThreadHistoryDto> GetUserThreadsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının yazdığı post'ları getirir (sayfalama ile)
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına öğe sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Post listesi</returns>
    Task<UserPostHistoryDto> GetUserPostsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının profil resmini günceller
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="imageUrl">Yeni profil resmi URL'i</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Güncellenmiş profil URL'i</returns>
    Task<string?> UpdateProfileImageAsync(int userId, string imageUrl, CancellationToken cancellationToken = default);
}
