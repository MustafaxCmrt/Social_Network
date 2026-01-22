using Application.DTOs.Moderation;
using FluentValidation;

namespace Application.Validations.Moderation;

/// <summary>
/// BanUserDto için validation kuralları
/// </summary>
public class BanUserDtoValidator : AbstractValidator<BanUserDto>
{
    public BanUserDtoValidator()
    {
        // UserId pozitif olmalı
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir kullanıcı ID'si girilmelidir.");
        
        // Reason zorunlu ve max 500 karakter
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Yasaklama sebebi girilmelidir.")
            .MaximumLength(500)
            .WithMessage("Yasaklama sebebi en fazla 500 karakter olabilir.");
        
        // ExpiresAt gelecekte olmalı (eğer set edilmişse)
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Yasak bitiş tarihi gelecekte olmalıdır.")
            .When(x => x.ExpiresAt.HasValue);
    }
}
