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
        // Tam olarak bir raporlama hedefi seçilmeli (User, Post veya Thread)
        RuleFor(x => x)
            .Must(x =>
                (x.ReportedUserId.HasValue ? 1 : 0) +
                (x.ReportedPostId.HasValue ? 1 : 0) +
                (x.ReportedThreadId.HasValue ? 1 : 0) == 1)
            .WithMessage("Tam olarak bir raporlama hedefi seçilmelidir (Kullanıcı, Post veya Thread).");
        
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
