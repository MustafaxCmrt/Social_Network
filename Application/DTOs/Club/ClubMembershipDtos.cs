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
/// Üyelik başvurusunu işleme DTO (onay/red/çıkarma)
/// </summary>
public record ProcessMembershipDto(
    int MembershipId,
    MembershipAction Action
);

/// <summary>
/// Üye rolünü değiştirme DTO (Başkan ataması dahil)
/// </summary>
public record UpdateMemberRoleDto(
    int MembershipId,
    ClubRole NewRole
);
