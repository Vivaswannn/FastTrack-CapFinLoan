namespace CapFinLoan.SharedKernel.DTOs;

/// <summary>
/// Incoming pagination parameters for list endpoints.
/// Bind with [FromQuery] to accept ?pageNumber=1&amp;pageSize=10 query strings.
/// </summary>
public class PaginationFilter
{
    /// <summary>Gets or sets the 1-based page number. Defaults to 1.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets the maximum number of items per page. Defaults to 10.</summary>
    public int PageSize { get; set; } = 10;
}
