using Application.DTOs.Moderation;

namespace Application.Services.Abstractions;

/// <summary>
/// Moderasyon işlemleri için servis interface'i
/// </summary>
public interface IModerationService
{
    /// <summary>
    /// Kullanıcıyı yasaklar (geçici veya kalıcı)
    /// </summary>
    /// <param name="dto">Ban bilgileri</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Oluşturulan ban kaydı</returns>
    Task<UserBanDto> BanUserAsync(BanUserDto dto, int adminUserId);

    /// <summary>
    /// Kullanıcının yasağını kaldırır
    /// </summary>
    /// <param name="userId">Yasağı kaldırılacak kullanıcı ID'si</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> UnbanUserAsync(int userId, int adminUserId);

    /// <summary>
    /// Kullanıcıyı susturur (geçici)
    /// </summary>
    /// <param name="dto">Mute bilgileri</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Oluşturulan mute kaydı</returns>
    Task<UserMuteDto> MuteUserAsync(MuteUserDto dto, int adminUserId);

    /// <summary>
    /// Kullanıcının susturmasını kaldırır
    /// </summary>
    /// <param name="userId">Susturması kaldırılacak kullanıcı ID'si</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> UnmuteUserAsync(int userId, int adminUserId);

    /// <summary>
    /// Thread'i kilitler (yeni post eklenemez)
    /// </summary>
    /// <param name="threadId">Kilitlenecek thread ID'si</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> LockThreadAsync(int threadId, int adminUserId);

    /// <summary>
    /// Thread kilidini açar
    /// </summary>
    /// <param name="threadId">Kilidi açılacak thread ID'si</param>
    /// <param name="adminUserId">İşlemi yapan admin ID'si</param>
    /// <returns>Başarılı ise true</returns>
    Task<bool> UnlockThreadAsync(int threadId, int adminUserId);

    /// <summary>
    /// Kullanıcının aktif bir yasağı var mı kontrol eder
    /// </summary>
    /// <param name="userId">Kontrol edilecek kullanıcı ID'si</param>
    /// <returns>Yasaklı ise true, aktif ban bilgisi</returns>
    Task<(bool IsBanned, UserBanDto? ActiveBan)> IsUserBannedAsync(int userId);

    /// <summary>
    /// Kullanıcının aktif bir susturması var mı kontrol eder
    /// </summary>
    /// <param name="userId">Kontrol edilecek kullanıcı ID'si</param>
    /// <returns>Susturulmuş ise true, aktif mute bilgisi</returns>
    Task<(bool IsMuted, UserMuteDto? ActiveMute)> IsUserMutedAsync(int userId);

    /// <summary>
    /// Kullanıcının ban geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <returns>Ban geçmişi listesi</returns>
    Task<IEnumerable<UserBanDto>> GetUserBanHistoryAsync(int userId);

    /// <summary>
    /// Kullanıcının mute geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <returns>Mute geçmişi listesi</returns>
    Task<IEnumerable<UserMuteDto>> GetUserMuteHistoryAsync(int userId);

    /// <summary>
    /// Kullanıcı adı veya isim-soyisme göre kullanıcı arar (Admin paneli için)
    /// </summary>
    /// <param name="searchTerm">Aranacak kelime (username, firstName, lastName)</param>
    /// <returns>Eşleşen kullanıcılar</returns>
    Task<IEnumerable<Application.DTOs.Search.SearchUserResultDto>> SearchUsersAsync(string searchTerm);

    /// <summary>
    /// Thread başlığına göre thread arar (Admin paneli için)
    /// </summary>
    /// <param name="searchTerm">Aranacak kelime (thread title)</param>
    /// <returns>Eşleşen thread'ler</returns>
    Task<IEnumerable<Application.DTOs.Search.SearchThreadResultDto>> SearchThreadsAsync(string searchTerm);
}
