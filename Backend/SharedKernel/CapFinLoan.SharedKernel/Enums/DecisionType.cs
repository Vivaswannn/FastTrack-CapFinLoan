namespace CapFinLoan.SharedKernel.Enums;

/// <summary>
/// Represents the type of admin decision made on a loan application.
/// Stored as string in the database via .HasConversion&lt;string&gt;().
/// </summary>
public enum DecisionType
{
    Approved,
    Rejected
}
