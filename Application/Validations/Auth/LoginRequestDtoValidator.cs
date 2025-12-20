using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validations.Auth;

/// <summary>
/// LoginRequestDto için validation kuralları
/// FluentValidation kullanarak gelen verileri doğrular
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        // UsernameOrEmail alanı için validasyonlar
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("Kullanıcı adı veya email adresi boş olamaz")
            .MaximumLength(100)
            .WithMessage("Kullanıcı adı veya email en fazla 100 karakter olabilir")
            .Must(BeValidUsernameOrEmail)
            .WithMessage("Geçerli bir kullanıcı adı (min 4 karakter) veya email adresi giriniz");

        // Password alanı için validasyonlar
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Şifre boş olamaz")
            .MinimumLength(6)
            .WithMessage("Şifre en az 6 karakter olmalıdır")
            .MaximumLength(100)
            .WithMessage("Şifre en fazla 100 karakter olabilir");
    }

    /// <summary>
    /// Kullanıcı adı veya email validasyonu
    /// Eğer @ işareti varsa email formatı kontrol edilir
    /// Yoksa kullanıcı adı olarak minimum 4 karakter kontrolü yapılır
    /// </summary>
    private bool BeValidUsernameOrEmail(string usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return false;

        // Email gibi görünüyorsa (@ işareti varsa)
        if (usernameOrEmail.Contains('@'))
        {
            // Email validation: basit kontrol
            // Format: xxx@xxx.xxx
            var parts = usernameOrEmail.Split('@');
            if (parts.Length != 2)
                return false;

            // @ işaretinden önce ve sonra karakter olmalı
            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                return false;

            // Domain kısmında nokta olmalı (xxx.com gibi)
            if (!parts[1].Contains('.'))
                return false;

            return true;
        }

        // Username ise minimum 4 karakter olmalı
        return usernameOrEmail.Length >= 4;
    }
}
