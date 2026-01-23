using Application.DTOs.PasswordReset;
using FluentValidation;

namespace Application.Validations.PasswordReset;

public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token gereklidir.")
            .Length(36).WithMessage("Geçersiz token formatı."); // GUID length
        
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gereklidir.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
            .MaximumLength(50).WithMessage("Şifre en fazla 50 karakter olabilir.");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Şifre tekrarı gereklidir.")
            .Equal(x => x.NewPassword).WithMessage("Şifreler eşleşmiyor.");
    }
}
