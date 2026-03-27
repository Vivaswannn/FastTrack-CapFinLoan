namespace CapFinLoan.ApplicationService.DTOs.Responses
{
    /// <summary>
    /// Response DTO representing a loan application.
    /// Enums are returned as strings for client readability.
    /// StatusHistories is populated for single-get and empty for list endpoints.
    /// </summary>
    public class LoanApplicationResponseDto
    {
        /// <summary>Unique identifier of this application.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>ID of the applicant who created this application.</summary>
        public Guid UserId { get; set; }

        /// <summary>Type of loan (as string).</summary>
        public string LoanType { get; set; } = string.Empty;

        /// <summary>Requested loan amount in INR.</summary>
        public decimal LoanAmount { get; set; }

        /// <summary>Repayment tenure in months.</summary>
        public int TenureMonths { get; set; }

        /// <summary>Purpose or reason for the loan.</summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Applicant full name.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Applicant email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Applicant mobile number.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Applicant date of birth.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Applicant residential address.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Name of current employer.</summary>
        public string EmployerName { get; set; } = string.Empty;

        /// <summary>Employment type.</summary>
        public string EmploymentType { get; set; } = string.Empty;

        /// <summary>Current job title or designation.</summary>
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>Monthly gross income in INR.</summary>
        public decimal MonthlyIncome { get; set; }

        /// <summary>Total years of work experience.</summary>
        public int YearsOfExperience { get; set; }

        /// <summary>Employer office address.</summary>
        public string EmployerAddress { get; set; } = string.Empty;

        /// <summary>Current status of the application (as string).</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>When this application was first created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>When this application was last modified.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>When applicant clicked Submit.</summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// Full audit trail of status changes.
        /// Populated for single-get; empty list for paginated list (performance).
        /// </summary>
        public List<StatusHistoryResponseDto> StatusHistories { get; set; } = [];
    }
}
