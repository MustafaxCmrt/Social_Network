using Application.DTOs.Category;
using FluentValidation;

namespace Application.Validations.Category;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir kategori ID'si giriniz.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Kategori başlığı zorunludur.")
            .MinimumLength(3).WithMessage("Kategori başlığı en az 3 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Kategori başlığı en fazla 100 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
