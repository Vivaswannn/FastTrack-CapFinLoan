using CapFinLoan.SharedKernel.Enums;
using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.ApplicationService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for creating a new loan application draft.
    /// </summary>
    public class CreateLoanApplicationDto
    {
        /// <summary>Type of loan being applied for.</summary>
        public LoanType LoanType { get; set; }

        /// <summary>Requested loan amount in INR (10,000 – 1,00,00,000).</summary>
        public decimal LoanAmount { get; set; }

        /// <summary>Repayment tenure in months (6 – 360).</summary>
        public int TenureMonths { get; set; }

        /// <summary>Purpose or reason for the loan.</summary>
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Applicant full name.</summary>
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Applicant email address.</summary>
        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Applicant mobile number.</summary>
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>Applicant date of birth.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Applicant residential address.</summary>
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        /// <summary>Name of current employer.</summary>
        [MaxLength(100)]
        public string EmployerName { get; set; } = string.Empty;

        /// <summary>Employment type: Salaried or Self-Employed.</summary>
        [MaxLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        /// <summary>Current job title or designation.</summary>
        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>Monthly gross income in INR.</summary>
        public decimal MonthlyIncome { get; set; }

        /// <summary>Total years of work experience.</summary>
        public int YearsOfExperience { get; set; }

        /// <summary>Employer office address.</summary>
        [MaxLength(500)]
        public string EmployerAddress { get; set; } = string.Empty;
    }
}
