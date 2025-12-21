using Application.DTOs.User;
using FluentValidation;

namespace Application.Validations.User;

/// <summary>
/// CreateUserDto için validation kuralları
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
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

        // Username - zorunlu, en az 3, en fazla 30 karakter, alfanumerik
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır")
            .MaximumLength(30).WithMessage("Kullanıcı adı en fazla 30 karakter olabilir")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı sadece harf, rakam ve _ içerebilir");

        // Email - zorunlu, geçerli email formatı
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email adresi zorunludur")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(100).WithMessage("Email adresi en fazla 100 karakter olabilir");

        // Password - zorunlu, en az 6 karakter
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir");

        // Role - zorunlu, geçerli rol değeri
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol zorunludur")
            .Must(role => role == "User" || role == "Moderator" || role == "Admin")
            .WithMessage("Geçerli rol değerleri: User, Moderator, Admin");
    }
}
