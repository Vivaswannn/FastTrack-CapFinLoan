namespace CapFinLoan.SharedKernel.Enums;

/// <summary>
/// Represents the type of loan being applied for.
/// Stored as string in the database via .HasConversion&lt;string&gt;().
/// </summary>
public enum LoanType
{
    Personal,
    Home,
    Vehicle,
    Education,
    Business
}
