using FluentValidation;
using SampleASP.Models;

namespace SampleASP.Validators;

public class ApiRequestValidator : AbstractValidator<ApiRequest>
{
    public ApiRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty()
            .WithMessage("Selector cannot be empty")
            .WithErrorCode("SELECTOR_EMPTY");

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("Attribute cannot be empty")
            .WithErrorCode("ATTRIBUTE_EMPTY");

        RuleFor(x => x.Url_b64)
            .NotEmpty()
            .WithMessage("Url_b64 cannot be empty")
            .WithErrorCode("URL_B64_EMPTY");

        RuleFor(x => x.Page_b64)
            .NotEmpty()
            .WithMessage("Page_b64 cannot be empty")
            .WithErrorCode("PAGE_B64_EMPTY");

        RuleFor(x => x.Encrypted_text_bytes_b64)
            .NotEmpty()
            .WithMessage("Encrypted_text_bytes_b64 cannot be empty")
            .WithErrorCode("ENCRYPTED_TEXT_EMPTY");

        RuleFor(x => x.Key_bytes_b64)
            .NotEmpty()
            .WithMessage("Key_bytes_b64 cannot be empty")
            .WithErrorCode("KEY_EMPTY");
    }
}
