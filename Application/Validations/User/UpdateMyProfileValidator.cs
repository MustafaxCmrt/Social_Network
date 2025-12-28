using Application.DTOs.User;
using FluentValidation;

namespace Application.Validations.User;

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileDto>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı zorunludur")
            .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı zorunludur")
            .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir");

        RuleFor(x => x.Username)
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı sadece harf, rakam ve alt çizgi içerebilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(255).WithMessage("Email en fazla 255 karakter olabilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.ProfileImg)
            .MaximumLength(500).WithMessage("Profil resmi URL'i en fazla 500 karakter olabilir")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImg));

        RuleFor(x => x.NewPassword)
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));

        // NewPassword girilmişse NewPasswordConfirm zorunlu ve eşit olmalı
        RuleFor(x => x.NewPasswordConfirm)
            .NotEmpty().WithMessage("Yeni şifre tekrarını girmelisiniz")
            .Equal(x => x.NewPassword).WithMessage("Şifreler uyuşmuyor")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));

        // NewPassword girilmişse CurrentPassword zorunlu
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Şifre değiştirmek için mevcut şifrenizi girmelisiniz")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}
