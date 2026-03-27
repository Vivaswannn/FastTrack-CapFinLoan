using CapFinLoan.AdminService.DTOs.Requests;
using FluentValidation;

namespace CapFinLoan.AdminService.Validators
{
    /// <summary>
    /// Validates MakeDecisionDto.
    /// Approval requires full sanction terms and financial parameters.
    /// Rejection requires a detailed reason.
    /// </summary>
    public class MakeDecisionValidator : AbstractValidator<MakeDecisionDto>
    {
        public MakeDecisionValidator()
        {
            RuleFor(x => x.DecisionType)
                .NotEmpty()
                .WithMessage("DecisionType is required.")
                .Must(t => t == "Approved" || t == "Rejected")
                .WithMessage("DecisionType must be 'Approved' or 'Rejected'.");

            RuleFor(x => x.Remarks)
                .NotEmpty()
                .WithMessage("Remarks are required.")
                .MaximumLength(1000)
                .WithMessage("Remarks must not exceed 1000 characters.");

            // Rules that apply only when approving
            When(x => x.DecisionType == "Approved", () =>
            {
                RuleFor(x => x.SanctionTerms)
                    .NotEmpty()
                    .WithMessage("Sanction terms are required for approval.");

                RuleFor(x => x.LoanAmountApproved)
                    .NotNull()
                    .WithMessage("Approved loan amount is required.")
                    .GreaterThan(0)
                    .WithMessage("Approved loan amount must be greater than 0.");

                RuleFor(x => x.InterestRate)
                    .NotNull()
                    .WithMessage("Interest rate is required for approval.")
                    .InclusiveBetween(1, 36)
                    .WithMessage("Interest rate must be between 1% and 36%.");

                RuleFor(x => x.TenureMonths)
                    .NotNull()
                    .WithMessage("Tenure is required for approval.")
                    .InclusiveBetween(6, 360)
                    .WithMessage("Tenure must be between 6 and 360 months.");
            });

            // Rules that apply only when rejecting
            When(x => x.DecisionType == "Rejected", () =>
            {
                RuleFor(x => x.Remarks)
                    .MinimumLength(10)
                    .WithMessage("Please provide a detailed reason (min 10 characters).");
            });
        }
    }
}
