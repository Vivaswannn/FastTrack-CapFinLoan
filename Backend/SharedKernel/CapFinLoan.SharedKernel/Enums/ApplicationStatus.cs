namespace CapFinLoan.SharedKernel.Enums;

/// <summary>
/// Represents the lifecycle status of a loan application.
/// Stored as string in the database via .HasConversion&lt;string&gt;().
/// </summary>
public enum ApplicationStatus
{
    Draft,
    Submitted,
    DocsPending,
    DocsVerified,
    UnderReview,
    Approved,
    Rejected,
    Closed
}
