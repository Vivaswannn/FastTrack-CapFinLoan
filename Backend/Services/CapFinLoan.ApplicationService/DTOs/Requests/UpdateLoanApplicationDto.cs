using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.ApplicationService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for updating an existing loan application draft.
    /// All fields are optional — only non-null fields will be updated.
    /// Cannot update Status through this DTO.
    /// </summary>
    public class UpdateLoanApplicationDto
    {
        /// <summary>Type of loan being applied for.</summary>
        public LoanType? LoanType { get; set; }

        /// <summary>Requested loan amount in INR.</summary>
        public decimal? LoanAmount { get; set; }

        /// <summary>Repayment tenure in months.</summary>
        public int? TenureMonths { get; set; }

        /// <summary>Purpose or reason for the loan.</summary>
        public string? Purpose { get; set; }

        /// <summary>Applicant full name.</summary>
        public string? FullName { get; set; }

        /// <summary>Applicant email address.</summary>
        public string? Email { get; set; }

        /// <summary>Applicant mobile number.</summary>
        public string? Phone { get; set; }

        /// <summary>Applicant date of birth.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Applicant residential address.</summary>
        public string? Address { get; set; }

        /// <summary>Name of current employer.</summary>
        public string? EmployerName { get; set; }

        /// <summary>Employment type: Salaried or Self-Employed.</summary>
        public string? EmploymentType { get; set; }

        /// <summary>Current job title or designation.</summary>
        public string? JobTitle { get; set; }

        /// <summary>Monthly gross income in INR.</summary>
        public decimal? MonthlyIncome { get; set; }

        /// <summary>Total years of work experience.</summary>
        public int? YearsOfExperience { get; set; }

        /// <summary>Employer office address.</summary>
        public string? EmployerAddress { get; set; }
    }
}
