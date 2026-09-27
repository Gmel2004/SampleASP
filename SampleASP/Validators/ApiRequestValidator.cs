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
            .WithErrorCode(ErrorCode.SELECTOR_EMPTY.ToString());

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("Attribute cannot be empty")
            .WithErrorCode(ErrorCode.ATTRIBUTE_EMPTY.ToString());

        RuleFor(x => x.UrlB64)
            .NotEmpty()
            .WithMessage("Url_b64 cannot be empty")
            .WithErrorCode(ErrorCode.URL_B64_EMPTY.ToString());

        RuleFor(x => x.PageB64)
            .NotEmpty()
            .WithMessage("Page_b64 cannot be empty")
            .WithErrorCode(ErrorCode.PAGE_B64_EMPTY.ToString());

        RuleFor(x => x.EncryptedTextBytesB64)
            .NotEmpty()
            .WithMessage("Encrypted_text_bytes_b64 cannot be empty")
            .WithErrorCode(ErrorCode.ENCRYPTED_TEXT_EMPTY.ToString());

        RuleFor(x => x.KeyBytesB64)
            .NotEmpty()
            .WithMessage("Key_bytes_b64 cannot be empty")
            .WithErrorCode(ErrorCode.KEY_EMPTY.ToString());
    }
}
