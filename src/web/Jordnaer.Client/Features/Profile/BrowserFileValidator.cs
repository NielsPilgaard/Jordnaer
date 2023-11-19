using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Jordnaer.Client.Features.Profile;

public class BrowserFileValidator : AbstractValidator<IBrowserFile>
{
    public const int MaxFileSize = 2_097_152;
    private static readonly string[] _validContentTypes = { "image/jpeg", "image/png" };
    public BrowserFileValidator()
    {
        // Stop validation if this fails
        RuleFor(file => file).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(x => x!.ContentType)
            .Must(contentType => _validContentTypes.Contains(contentType))
            .WithMessage("Kun JPEG og PNG billeder godtages.");

        // 2 (MB) * 1024 (bytes -> kilobytes) * 1024 (kilobytes -> megabytes)
        RuleFor(file => file!.Size).LessThanOrEqualTo(MaxFileSize).WithMessage("Maks filstørrelse er 2 MB");
    }
}
