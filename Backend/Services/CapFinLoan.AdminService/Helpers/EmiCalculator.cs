namespace CapFinLoan.AdminService.Helpers
{
    /// <summary>
    /// Calculates monthly EMI for approved loans.
    /// Formula: EMI = P * r * (1+r)^n / ((1+r)^n - 1)
    /// where P = principal, r = monthly rate, n = tenure months.
    /// </summary>
    public static class EmiCalculator
    {
        /// <summary>
        /// Calculates monthly EMI amount.
        /// </summary>
        /// <param name="principal">Loan principal amount in INR.</param>
        /// <param name="annualInterestRate">Annual rate, e.g. 10.5 for 10.5%.</param>
        /// <param name="tenureMonths">Repayment period in months.</param>
        /// <returns>Rounded monthly EMI to 2 decimal places.</returns>
        public static decimal CalculateEmi(
            decimal principal,
            decimal annualInterestRate,
            int tenureMonths)
        {
            if (annualInterestRate == 0)
                return Math.Round(principal / tenureMonths, 2);

            decimal monthlyRate = annualInterestRate / 12 / 100;
            decimal factor = (decimal)Math.Pow(
                (double)(1 + monthlyRate), tenureMonths);
            decimal emi = principal * monthlyRate * factor / (factor - 1);
            return Math.Round(emi, 2);
        }
    }
}
