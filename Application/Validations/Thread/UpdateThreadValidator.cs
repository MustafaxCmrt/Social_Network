using Application.DTOs.Thread;
using FluentValidation;

namespace Application.Validations.Thread;

public class UpdateThreadValidator : AbstractValidator<UpdateThreadDto>
{
    public UpdateThreadValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir konu ID'si giriniz.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Konu başlığı zorunludur.")
            .MinimumLength(3).WithMessage("Konu başlığı en az 3 karakter olmalıdır.")
            .MaximumLength(150).WithMessage("Konu başlığı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik zorunludur.")
            .MinimumLength(10).WithMessage("İçerik en az 10 karakter olmalıdır.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Geçerli bir kategori ID'si giriniz.");
    }
}
