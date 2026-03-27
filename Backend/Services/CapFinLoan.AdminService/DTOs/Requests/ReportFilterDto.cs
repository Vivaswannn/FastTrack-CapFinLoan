namespace CapFinLoan.AdminService.DTOs.Requests
{
    /// <summary>
    /// Optional date range and pagination parameters for report queries.
    /// </summary>
    public class ReportFilterDto
    {
        /// <summary>Inclusive start date filter (optional).</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Inclusive end date filter (optional).</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>1-based page number.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Number of records per page.</summary>
        public int PageSize { get; set; } = 10;
    }
}
