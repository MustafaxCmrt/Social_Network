using Application.DTOs.Post;
using FluentValidation;

namespace Application.Validations.Post;

public class MarkSolutionValidator : AbstractValidator<MarkSolutionDto>
{
    public MarkSolutionValidator()
    {
        RuleFor(x => x.ThreadId)
            .GreaterThan(0).WithMessage("Geçerli bir konu ID'si giriniz.");

        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("Geçerli bir yorum ID'si giriniz.");
    }
}
