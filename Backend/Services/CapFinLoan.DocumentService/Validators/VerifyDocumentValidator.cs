using CapFinLoan.DocumentService.DTOs.Requests;
using FluentValidation;

namespace CapFinLoan.DocumentService.Validators
{
    /// <summary>
    /// Validates the VerifyDocumentDto.
    /// Enforces that a rejection reason is provided when IsVerified is false.
    /// </summary>
    public class VerifyDocumentValidator : AbstractValidator<VerifyDocumentDto>
    {
        public VerifyDocumentValidator()
        {
            RuleFor(x => x.VerificationRemarks)
                .NotEmpty()
                .WithMessage("Please provide a reason for rejecting this document.")
                .When(x => !x.IsVerified);
        }
    }
}
