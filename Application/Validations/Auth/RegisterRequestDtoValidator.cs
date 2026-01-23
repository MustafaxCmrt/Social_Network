using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validations.Auth;

/// <summary>
/// RegisterRequestDto için validation kuralları
/// </summary>
public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        // FirstName validasyonları
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Ad boş olamaz")
            .MinimumLength(2)
            .WithMessage("Ad en az 2 karakter olmalıdır")
            .MaximumLength(50)
            .WithMessage("Ad en fazla 50 karakter olabilir")
            .Matches("^[a-zA-ZğüşıöçĞÜŞİÖÇ ]+$")
            .WithMessage("Ad sadece harf içerebilir");

        // LastName validasyonları
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Soyad boş olamaz")
            .MinimumLength(2)
            .WithMessage("Soyad en az 2 karakter olmalıdır")
            .MaximumLength(50)
            .WithMessage("Soyad en fazla 50 karakter olabilir")
            .Matches("^[a-zA-ZğüşıöçĞÜŞİÖÇ ]+$")
            .WithMessage("Soyad sadece harf içerebilir");

        // Username validasyonları
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Kullanıcı adı boş olamaz")
            .MinimumLength(4)
            .WithMessage("Kullanıcı adı en az 4 karakter olmalıdır")
            .MaximumLength(30)
            .WithMessage("Kullanıcı adı en fazla 30 karakter olabilir")
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Kullanıcı adı sadece harf, rakam ve alt çizgi (_) içerebilir");

        // Email validasyonları
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email adresi boş olamaz")
            .EmailAddress()
            .WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(100)
            .WithMessage("Email adresi en fazla 100 karakter olabilir");

        // Password validasyonları - Güçlü şifre gereksinimleri
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Şifre boş olamaz")
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalıdır")
            .MaximumLength(100)
            .WithMessage("Şifre en fazla 100 karakter olabilir")
            .Matches("[A-Z]")
            .WithMessage("Şifre en az bir büyük harf içermelidir")
            .Matches("[a-z]")
            .WithMessage("Şifre en az bir küçük harf içermelidir")
            .Matches("[0-9]")
            .WithMessage("Şifre en az bir rakam içermelidir")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Şifre en az bir özel karakter içermelidir (!@#$%^&* vb.)");

        // ConfirmPassword validasyonları
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Şifre tekrarı boş olamaz")
            .Equal(x => x.Password)
            .WithMessage("Şifreler eşleşmiyor");
    }
}
