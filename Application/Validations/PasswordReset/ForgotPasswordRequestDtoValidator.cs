using Application.DTOs.PasswordReset;
using FluentValidation;

namespace Application.Validations.PasswordReset;

public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
            .MaximumLength(100).WithMessage("Email adresi en fazla 100 karakter olabilir.");
    }
}
