using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.SharedKernel.Enums;
using FluentValidation;

namespace CapFinLoan.ApplicationService.Validators
{
    /// <summary>
    /// FluentValidation validator for UpdateApplicationStatusDto.
    /// Ensures status is valid and remarks are provided when rejecting.
    /// </summary>
    public class UpdateApplicationStatusValidator : AbstractValidator<UpdateApplicationStatusDto>
    {
        /// <summary>Initializes all validation rules.</summary>
        public UpdateApplicationStatusValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage("Invalid application status specified.");

            RuleFor(x => x.Remarks)
                .NotEmpty()
                .When(x => x.NewStatus == ApplicationStatus.Rejected)
                .WithMessage("Remarks are required when rejecting an application.");
        }
    }
}
