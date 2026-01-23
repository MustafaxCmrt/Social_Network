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
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir (!@#$%^&* vb.).");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Şifre tekrarı gereklidir.")
            .Equal(x => x.NewPassword).WithMessage("Şifreler eşleşmiyor.");
    }
}
