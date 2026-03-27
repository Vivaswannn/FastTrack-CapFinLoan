namespace CapFinLoan.SharedKernel.Enums;

/// <summary>
/// Represents the type of KYC or supporting document uploaded by an applicant.
/// Stored as string in the database via .HasConversion&lt;string&gt;().
/// </summary>
public enum DocumentType
{
    AadhaarCard,
    PAN,
    Passport,
    SalarySlip,
    BankStatement,
    ITReturn,
    UtilityBill,
    Other
}
