using CapFinLoan.ApplicationService.DTOs.Requests;
using FluentValidation;

namespace CapFinLoan.ApplicationService.Validators
{
    /// <summary>
    /// FluentValidation validator for CreateLoanApplicationDto.
    /// Enforces all business rules for creating a loan application draft.
    /// </summary>
    public class CreateLoanApplicationValidator : AbstractValidator<CreateLoanApplicationDto>
    {
        /// <summary>Initializes all validation rules.</summary>
        public CreateLoanApplicationValidator()
        {
            RuleFor(x => x.LoanType)
                .IsInEnum()
                .When(x => x.LoanType != 0)
                .WithMessage("Invalid loan type specified.");

            RuleFor(x => x.LoanAmount)
                .InclusiveBetween(10000m, 10000000m)
                .When(x => x.LoanAmount != 0)
                .WithMessage("Loan amount must be between ₹10,000 and ₹1,00,00,000.");

            RuleFor(x => x.TenureMonths)
                .InclusiveBetween(6, 360)
                .When(x => x.TenureMonths != 0)
                .WithMessage("Tenure must be between 6 months and 30 years.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

            RuleFor(x => x.Phone)
                .Matches(@"^[6-9]\d{9}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Phone must be a valid 10-digit Indian mobile number starting with 6-9.");

            RuleFor(x => x.MonthlyIncome)
                .GreaterThan(0)
                .When(x => x.MonthlyIncome != 0)
                .WithMessage("Monthly income must be greater than 0.");

            RuleFor(x => x.YearsOfExperience)
                .GreaterThanOrEqualTo(0)
                .When(x => x.YearsOfExperience != 0)
                .WithMessage("Years of experience must be 0 or greater.");

            RuleFor(x => x.EmploymentType)
                .Must(t => t == "Salaried" || t == "Self-Employed")
                .When(x => !string.IsNullOrEmpty(x.EmploymentType))
                .WithMessage("Employment type must be 'Salaried' or 'Self-Employed'.");
        }
    }
}
