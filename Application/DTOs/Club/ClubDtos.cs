using Domain.Enums;

namespace Application.DTOs.Club;

/// <summary>
/// Yeni kulüp oluşturma için DTO (Admin - doğrudan oluşturma)
/// </summary>
public record CreateClubDto(
    string Name,
    string Description,
    bool IsPublic = true,
    bool RequiresApproval = true
);

/// <summary>
/// Yeni kulüp açma başvurusu için DTO
/// </summary>
public record CreateClubRequestDto(
    string Name,
    string Description,
    string Purpose
);

/// <summary>
/// Kulüp başvuru listesi için DTO
/// </summary>
public record ClubRequestListDto(
    int Id,
    string Name,
    string Description,
    string Purpose,
    ClubRequestStatus Status,
    int RequestedByUserId,
    string RequestedByUsername,
    DateTime CreatedAt,
    int? ReviewedByUserId,
    string? ReviewedByUsername,
    DateTime? ReviewedAt,
    string? RejectionReason
);

/// <summary>
/// Kulüp başvurusu inceleme (onay/red) için DTO
/// </summary>
public record ReviewClubRequestDto(
    int RequestId,
    bool Approve,
    string? RejectionReason = null
);

/// <summary>
/// Kulüp detay DTO
/// </summary>
public record ClubDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? BannerUrl,
    bool IsPublic,
    bool RequiresApproval,
    int MemberCount,
    int FounderId,
    string FounderUsername,
    DateTime CreatedAt,
    ClubApplicationStatus ApplicationStatus,
    string? RejectionReason = null,
    DateTime? ReviewedAt = null,
    bool? IsMember = null,
    ClubRole? CurrentUserRole = null,
    MembershipStatus? CurrentUserStatus = null
);

/// <summary>
/// Kulüp listesi için özet DTO
/// </summary>
public record ClubListDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    int MemberCount,
    bool IsPublic,
    int FounderId,
    string FounderUsername,
    ClubApplicationStatus ApplicationStatus,
    string? RejectionReason = null,
    DateTime? ReviewedAt = null
);

/// <summary>
/// Kulüp güncelleme DTO
/// </summary>
public record UpdateClubDto(
    int Id,
    string? Name,
    string? Description,
    string? LogoUrl,
    string? BannerUrl,
    bool? IsPublic,
    bool? RequiresApproval
);

/// <summary>
/// Kulüp başvuru durumunu güncelleme DTO (Admin/Moderator)
/// </summary>
public record UpdateClubApplicationStatusDto(
    ClubApplicationStatus Status,
    string? RejectionReason = null
);

/// <summary>
/// Kullanıcının kulüplerini listelerken kullanılır
/// </summary>
public record MyClubDto(
    int ClubId,
    string ClubName,
    string ClubSlug,
    string? LogoUrl,
    ClubRole MyRole,
    MembershipStatus Status,
    DateTime? JoinedAt
);
