using Application.DTOs.Club;
using Application.DTOs.Common;
using Domain.Enums;

namespace Application.Services.Abstractions;

/// <summary>
/// Kulüp yönetimi servisi
/// </summary>
public interface IClubService
{
    // ==================== KULÜP BAŞVURU ====================

    Task<ClubRequestListDto> CreateClubRequestAsync(CreateClubRequestDto dto, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubRequestListDto>> GetPendingClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubRequestListDto>> GetMyClubRequestsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ClubRequestListDto> ReviewClubRequestAsync(ReviewClubRequestDto dto, CancellationToken cancellationToken = default);

    // ==================== KULÜP CRUD ====================

    Task<ClubDto> CreateClubDirectAsync(CreateClubDto dto, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubListDto>> GetAllClubsAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<ClubDto?> GetClubByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<ClubDto?> UpdateClubAsync(UpdateClubDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteClubAsync(int clubId, CancellationToken cancellationToken = default);
    Task<string?> UploadClubImageAsync(int clubId, string imageUrl, string imageType, CancellationToken cancellationToken = default);
    Task<bool> UpdateClubApplicationStatusAsync(int clubId, UpdateClubApplicationStatusDto dto, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubListDto>> GetUserClubApplicationsAsync(int page, int pageSize, ClubApplicationStatus? status = null, CancellationToken cancellationToken = default);

    // ==================== ÜYELİK ====================

    Task<ClubMemberDto> JoinClubAsync(JoinClubDto dto, CancellationToken cancellationToken = default);
    Task<bool> LeaveClubAsync(int clubId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubMemberDto>> GetClubMembersAsync(int clubId, int page, int pageSize, MembershipStatus? status = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ClubMemberDto>> GetPendingMembershipsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ClubMemberDto?> ProcessMembershipAsync(ProcessMembershipDto dto, CancellationToken cancellationToken = default);
    Task<ClubMemberDto?> UpdateMemberRoleAsync(UpdateMemberRoleDto dto, CancellationToken cancellationToken = default);
    Task<List<MyClubDto>> GetMyClubsAsync(CancellationToken cancellationToken = default);
}
