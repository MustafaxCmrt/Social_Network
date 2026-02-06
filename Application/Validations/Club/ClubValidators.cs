using Application.DTOs.Club;
using Domain.Enums;
using FluentValidation;

namespace Application.Validations.Club;

/// <summary>
/// Kulüp oluşturma validasyonu (Admin - doğrudan oluşturma)
/// </summary>
public class CreateClubValidator : AbstractValidator<CreateClubDto>
{
    public CreateClubValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kulüp adı boş olamaz")
            .MinimumLength(3).WithMessage("Kulüp adı en az 3 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Kulüp adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Kulüp açıklaması boş olamaz")
            .MinimumLength(20).WithMessage("Kulüp açıklaması en az 20 karakter olmalıdır")
            .MaximumLength(2000).WithMessage("Kulüp açıklaması en fazla 2000 karakter olabilir");
    }
}

/// <summary>
/// Kulüp açma başvurusu validasyonu
/// </summary>
public class CreateClubRequestValidator : AbstractValidator<CreateClubRequestDto>
{
    public CreateClubRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kulüp adı boş olamaz")
            .MinimumLength(3).WithMessage("Kulüp adı en az 3 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Kulüp adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Kulüp açıklaması boş olamaz")
            .MinimumLength(20).WithMessage("Kulüp açıklaması en az 20 karakter olmalıdır")
            .MaximumLength(1000).WithMessage("Kulüp açıklaması en fazla 1000 karakter olabilir");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Kulübün amacı boş olamaz")
            .MinimumLength(50).WithMessage("Kulübün amacı en az 50 karakter olmalıdır")
            .MaximumLength(2000).WithMessage("Kulübün amacı en fazla 2000 karakter olabilir");
    }
}

/// <summary>
/// Kulüp başvurusu inceleme validasyonu
/// </summary>
public class ReviewClubRequestValidator : AbstractValidator<ReviewClubRequestDto>
{
    public ReviewClubRequestValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage("Geçerli bir başvuru ID'si gereklidir");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().When(x => !x.Approve)
            .WithMessage("Başvuruyu reddederken sebep belirtmelisiniz")
            .MaximumLength(500).WithMessage("Red sebebi en fazla 500 karakter olabilir");
    }
}

/// <summary>
/// Kulüp güncelleme validasyonu
/// </summary>
public class UpdateClubValidator : AbstractValidator<UpdateClubDto>
{
    public UpdateClubValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir kulüp ID'si gereklidir");

        RuleFor(x => x.Name)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Kulüp adı en az 3 karakter olmalıdır")
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Kulüp adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Kulüp açıklaması en fazla 2000 karakter olabilir");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage("Logo URL en fazla 500 karakter olabilir");

        RuleFor(x => x.BannerUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.BannerUrl))
            .WithMessage("Banner URL en fazla 500 karakter olabilir");
    }
}

/// <summary>
/// Kulüp başvuru durumu güncelleme validasyonu (Admin/Moderator)
/// </summary>
public class UpdateClubApplicationStatusValidator : AbstractValidator<UpdateClubApplicationStatusDto>
{
    public UpdateClubApplicationStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Geçersiz durum değeri");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.Status == ClubApplicationStatus.Rejected)
            .WithMessage("Başvuru reddedilirken neden belirtilmelidir")
            .MaximumLength(500)
            .WithMessage("Ret nedeni en fazla 500 karakter olabilir");
    }
}

