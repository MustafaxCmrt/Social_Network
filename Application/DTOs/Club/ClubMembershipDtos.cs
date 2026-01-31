using Domain.Enums;

namespace Application.DTOs.Club;

/// <summary>
/// Kulübe katılma başvurusu DTO
/// </summary>
public record JoinClubDto(
    int ClubId,
    string? JoinNote = null
);

/// <summary>
/// Kulüp üyesi bilgisi DTO
/// </summary>
public record ClubMemberDto(
    int MembershipId,
    int UserId,
    string Username,
    string FirstName,
    string LastName,
    string? ProfileImg,
    ClubRole Role,
    MembershipStatus Status,
    DateTime? JoinedAt,
    string? JoinNote
);

/// <summary>
/// Üyelik başvurusunu işleme DTO (onay/red)
/// </summary>
public record ProcessMembershipDto(
    int MembershipId,
    bool Approve
);

/// <summary>
/// Üye rolünü değiştirme DTO
/// </summary>
public record UpdateMemberRoleDto(
    int MembershipId,
    ClubRole NewRole
);

/// <summary>
/// Üyeyi kulüpten çıkarma DTO
/// </summary>
public record KickMemberDto(
    int MembershipId
);

/// <summary>
/// Üyelik durumu kontrolü için response DTO
/// </summary>
public record MembershipStatusDto(
    bool IsMember,
    ClubRole? Role,
    MembershipStatus? Status
);
