using Application.DTOs.Report;
using Domain.Enums;
using FluentValidation;

namespace Application.Validations.Report;

/// <summary>
/// CreateReportDto için validation kuralları
/// </summary>
public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportDtoValidator()
    {
        // En az bir raporlama hedefi seçilmeli (User, Post veya Thread)
        RuleFor(x => x)
            .Must(x => x.ReportedUserId.HasValue || 
                      x.ReportedPostId.HasValue || 
                      x.ReportedThreadId.HasValue)
            .WithMessage("En az bir raporlama hedefi seçilmelidir (Kullanıcı, Post veya Thread).");
        
        // Reason zorunlu ve geçerli bir enum değeri olmalı
        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithMessage("Geçerli bir raporlama sebebi seçilmelidir.");
        
        // Description maksimum 1000 karakter olabilir
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Açıklama en fazla 1000 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
