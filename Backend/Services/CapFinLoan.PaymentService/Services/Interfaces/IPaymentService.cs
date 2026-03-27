using CapFinLoan.PaymentService.DTOs;
using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.PaymentService.Services.Interfaces
{
    /// <summary>Business logic interface for payment processing operations.</summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Simulates processing a loan disbursement.
        /// Creates a Payment record, updates status to Completed or Failed.
        /// </summary>
        Task<PaymentResponseDto> ProcessPaymentAsync(LoanApprovedEvent approvedEvent);

        /// <summary>Returns all payment records for a given application ID.</summary>
        Task<IReadOnlyList<PaymentResponseDto>> GetPaymentsByApplicationAsync(
            Guid applicationId);

        /// <summary>Returns a single payment record by ID, or null if not found.</summary>
        Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId);
    }
}
