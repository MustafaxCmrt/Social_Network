using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validations.Auth;

public class ResendVerificationEmailDtoValidator : AbstractValidator<ResendVerificationEmailDto>
{
    public ResendVerificationEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
            .MaximumLength(100).WithMessage("Email adresi en fazla 100 karakter olabilir.");
    }
}
