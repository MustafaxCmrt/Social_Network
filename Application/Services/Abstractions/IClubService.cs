using Application.DTOs.Club;
using Application.DTOs.Common;

namespace Application.Services.Abstractions;

/// <summary>
/// Kulüp yönetimi servisi
/// </summary>
public interface IClubService
{   
    /// <summary>
    /// Yeni kulüp açma başvurusu oluşturur
    /// </summary>
    Task<ClubRequestListDto> CreateClubRequestAsync(CreateClubRequestDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bekleyen kulüp başvurularını getirir (Moderatör/Admin için)
    /// </summary>
    Task<PagedResultDto<ClubRequestListDto>> GetPendingClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcının kendi kulüp başvurularını getirir
    /// </summary>
    Task<PagedResultDto<ClubRequestListDto>> GetMyClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp başvurusunu inceler (onaylar veya reddeder)
    /// </summary>
    Task<ClubRequestListDto> ReviewClubRequestAsync(ReviewClubRequestDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tüm kulüpleri listeler (sayfalı)
    /// </summary>
    Task<PagedResultDto<ClubListDto>> GetAllClubsAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp detayını getirir
    /// </summary>
    Task<ClubDto?> GetClubByIdAsync(int clubId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp detayını slug ile getirir
    /// </summary>
    Task<ClubDto?> GetClubBySlugAsync(string slug, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp bilgilerini günceller (Sadece başkan veya admin)
    /// </summary>
    Task<ClubDto?> UpdateClubAsync(UpdateClubDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulübü siler (Sadece admin)
    /// </summary>
    Task<bool> DeleteClubAsync(int clubId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp logosu yükler
    /// </summary>
    Task<string?> UploadClubLogoAsync(int clubId, string logoUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüp banner'ı yükler
    /// </summary>
    Task<string?> UploadClubBannerAsync(int clubId, string bannerUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulübe katılma başvurusu yapar
    /// </summary>
    Task<ClubMemberDto> JoinClubAsync(JoinClubDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulüpten ayrılır
    /// </summary>
    Task<bool> LeaveClubAsync(int clubId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kulübün üyelerini listeler
    /// </summary>
    Task<PagedResultDto<ClubMemberDto>> GetClubMembersAsync(int clubId, int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bekleyen üyelik başvurularını getirir (Kulüp yöneticileri için)
    /// </summary>
    Task<PagedResultDto<ClubMemberDto>> GetPendingMembersAsync(int clubId, int page, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Üyelik başvurusunu işler (onaylar veya reddeder)
    /// </summary>
    Task<ClubMemberDto?> ProcessMembershipAsync(ProcessMembershipDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Üye rolünü değiştirir
    /// </summary>
    Task<ClubMemberDto?> UpdateMemberRoleAsync(UpdateMemberRoleDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Üyeyi kulüpten çıkarır
    /// </summary>
    Task<bool> KickMemberAsync(KickMemberDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Başkanlığı devreder
    /// </summary>
    Task<bool> TransferPresidencyAsync(int clubId, int newPresidentUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcının üyelik durumunu kontrol eder
    /// </summary>
    Task<MembershipStatusDto> GetMembershipStatusAsync(int clubId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcının üye olduğu kulüpleri getirir
    /// </summary>
    Task<List<MyClubDto>> GetMyClubsAsync(CancellationToken cancellationToken = default);
}
