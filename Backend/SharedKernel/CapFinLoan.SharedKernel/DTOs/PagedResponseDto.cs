namespace CapFinLoan.SharedKernel.DTOs;

/// <summary>
/// Pagination wrapper returned by every list endpoint across all services.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public class PagedResponseDto<T>
{
    /// <summary>Gets or sets the items on the current page.</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>Gets or sets the total number of records across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the maximum number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Gets whether there is a page before the current one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets whether there is a page after the current one.</summary>
    public bool HasNextPage => Page < TotalPages;
}
