using Application.DTOs.Moderation;
using FluentValidation;

namespace Application.Validations.Moderation;

/// <summary>
/// MuteUserDto için validation kuralları
/// </summary>
public class MuteUserDtoValidator : AbstractValidator<MuteUserDto>
{
    public MuteUserDtoValidator()
    {
        // UserId pozitif olmalı
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir kullanıcı ID'si girilmelidir.");
        
        // Reason zorunlu ve max 500 karakter
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Susturma sebebi girilmelidir.")
            .MaximumLength(500)
            .WithMessage("Susturma sebebi en fazla 500 karakter olabilir.");
        
        // ExpiresAt zorunlu ve gelecekte olmalı
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Susturma bitiş tarihi gelecekte olmalıdır.");
    }
}
