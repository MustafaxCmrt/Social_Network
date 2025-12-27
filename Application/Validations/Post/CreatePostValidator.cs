using Application.DTOs.Post;
using FluentValidation;

namespace Application.Validations.Post;

public class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.ThreadId)
            .GreaterThan(0).WithMessage("Geçerli bir konu ID'si giriniz.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum içeriği zorunludur.")
            .MinimumLength(1).WithMessage("Yorum içeriği boş olamaz.")
            .MaximumLength(5000).WithMessage("Yorum içeriği en fazla 5000 karakter olabilir.");

        RuleFor(x => x.Img)
            .MaximumLength(500).WithMessage("Resim URL'i en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Img));
    }
}
