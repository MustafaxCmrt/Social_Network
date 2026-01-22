using Application.DTOs.Report;
using Domain.Enums;
using FluentValidation;

namespace Application.Validations.Report;

/// <summary>
/// UpdateReportStatusDto için validation kuralları
/// </summary>
public class UpdateReportStatusDtoValidator : AbstractValidator<UpdateReportStatusDto>
{
    public UpdateReportStatusDtoValidator()
    {
        // Status zorunlu ve geçerli bir enum değeri olmalı
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Geçerli bir durum seçilmelidir.");
        
        // Status Pending olmamalı (çünkü güncelleme yapıyoruz)
        RuleFor(x => x.Status)
            .NotEqual(ReportStatus.Pending)
            .WithMessage("Rapor durumu Pending olarak güncellenemez. Reviewed, Resolved veya Rejected seçiniz.");
        
        // AdminNote maksimum 500 karakter olabilir
        RuleFor(x => x.AdminNote)
            .MaximumLength(500)
            .WithMessage("Admin notu en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.AdminNote));
    }
}
