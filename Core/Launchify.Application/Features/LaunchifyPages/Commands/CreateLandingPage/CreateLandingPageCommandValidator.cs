using FluentValidation;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;

public class CreateLandingPageCommandValidator : AbstractValidator<CreateLandingPageCommand>
{
    public CreateLandingPageCommandValidator()
    {
        RuleFor(p => p.ProductName)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(60).WithMessage("Ürün adı çok uzun.");

        RuleFor(p => p.ProductDescription)
            .NotEmpty().WithMessage("Açıklama alanı zorunludur.")
            .MaximumLength(1000).WithMessage("AI brief'i en fazla 1000 karakter olabilir.");

        RuleFor(p => p.ContactEmail)
            .EmailAddress().WithMessage("Geçerli bir e-posta girin.")
            .MaximumLength(100)
            .When(p => !string.IsNullOrWhiteSpace(p.ContactEmail));

        RuleFor(p => p.DemoLink)
            .MaximumLength(255)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            .When(p => !string.IsNullOrWhiteSpace(p.DemoLink))
            .WithMessage("Geçerli bir URL girin.");
    }
}
