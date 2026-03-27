using CapFinLoan.AuthService.DTOs.Requests;
using FluentValidation;

namespace CapFinLoan.AuthService.Validators
{
    /// <summary>
    /// Validates LoginDto before processing login.
    /// </summary>
    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Please provide a valid email address.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
