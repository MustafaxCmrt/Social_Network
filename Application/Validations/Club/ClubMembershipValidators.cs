using Application.DTOs.Club;
using FluentValidation;

namespace Application.Validations.Club;

/// <summary>
/// Kulübe katılma validasyonu
/// </summary>
public class JoinClubValidator : AbstractValidator<JoinClubDto>
{
    public JoinClubValidator()
    {
        RuleFor(x => x.ClubId)
            .GreaterThan(0).WithMessage("Geçerli bir kulüp ID'si gereklidir");

        RuleFor(x => x.JoinNote)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.JoinNote))
            .WithMessage("Katılım notu en fazla 500 karakter olabilir");
    }
}

/// <summary>
/// Üyelik başvurusu işleme validasyonu (onay/red/çıkarma)
/// </summary>
public class ProcessMembershipValidator : AbstractValidator<ProcessMembershipDto>
{
    public ProcessMembershipValidator()
    {
        RuleFor(x => x.MembershipId)
            .GreaterThan(0).WithMessage("Geçerli bir üyelik ID'si gereklidir");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Geçerli bir işlem seçmelisiniz (Approve, Reject, Kick)");
    }
}

/// <summary>
/// Üye rol değiştirme validasyonu (başkanlık devri dahil)
/// </summary>
public class UpdateMemberRoleValidator : AbstractValidator<UpdateMemberRoleDto>
{
    public UpdateMemberRoleValidator()
    {
        RuleFor(x => x.MembershipId)
            .GreaterThan(0).WithMessage("Geçerli bir üyelik ID'si gereklidir");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("Geçerli bir rol seçmelisiniz");
    }
}
