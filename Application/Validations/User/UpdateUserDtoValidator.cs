using Application.DTOs.User;
using FluentValidation;

namespace Application.Validations.User;

/// <summary>
/// UpdateUserDto için validation kuralları
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        // FirstName - zorunlu, en az 2, en fazla 50 karakter
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı zorunludur")
            .MinimumLength(2).WithMessage("Ad en az 2 karakter olmalıdır")
            .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir");

        // LastName - zorunlu, en az 2, en fazla 50 karakter
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı zorunludur")
            .MinimumLength(2).WithMessage("Soyad en az 2 karakter olmalıdır")
            .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir");

        // Username - opsiyonel ama verilmişse geçerli olmalı
        RuleFor(x => x.Username)
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır")
            .MaximumLength(30).WithMessage("Kullanıcı adı en fazla 30 karakter olabilir")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı sadece harf, rakam ve _ içerebilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));

        // Email - opsiyonel ama verilmişse geçerli olmalı
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(100).WithMessage("Email adresi en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Password - opsiyonel ama verilmişse geçerli olmalı
        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}
