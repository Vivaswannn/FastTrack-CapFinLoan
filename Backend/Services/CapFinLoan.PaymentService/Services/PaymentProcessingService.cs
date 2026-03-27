using CapFinLoan.PaymentService.Data;
using CapFinLoan.PaymentService.DTOs;
using CapFinLoan.PaymentService.Models;
using CapFinLoan.PaymentService.Services.Interfaces;
using CapFinLoan.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.PaymentService.Services
{
    /// <summary>
    /// Implements payment processing with simulated disbursement logic.
    /// In a real system this would call a banking API.
    /// </summary>
    public class PaymentProcessingService : IPaymentService
    {
        private readonly PaymentDbContext _db;
        private readonly ILogger<PaymentProcessingService> _logger;

        public PaymentProcessingService(
            PaymentDbContext db,
            ILogger<PaymentProcessingService> logger)
        {
            _db     = db;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<PaymentResponseDto> ProcessPaymentAsync(
            LoanApprovedEvent approvedEvent)
        {
            _logger.LogInformation(
                "Processing payment for ApplicationId={ApplicationId} Amount={Amount}",
                approvedEvent.ApplicationId, approvedEvent.LoanAmountApproved);

            // Create payment in Pending state
            var payment = new Payment
            {
                ApplicationId = approvedEvent.ApplicationId,
                UserId        = approvedEvent.UserId,
                AmountDisbursed = approvedEvent.LoanAmountApproved,
                Status        = "Processing",
                CreatedAt     = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            // Simulate payment processing delay (replace with real banking API call)
            await Task.Delay(TimeSpan.FromMilliseconds(200));

            // Simulate 95% success rate
            bool success = new Random().Next(1, 101) <= 95;

            payment.Status          = success ? "Completed" : "Failed";
            payment.ProcessedAt     = DateTime.UtcNow;
            payment.ReferenceNumber = success
                ? $"DISB-{DateTime.UtcNow:yyyyMMdd}-{payment.PaymentId.ToString()[..8].ToUpper()}"
                : null;
            payment.Message = success
                ? $"Loan of ₹{approvedEvent.LoanAmountApproved:N0} disbursed successfully to applicant {approvedEvent.ApplicantName}."
                : "Disbursement failed: simulated bank gateway timeout. Please retry.";

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Payment {PaymentId} for ApplicationId={ApplicationId} — Status={Status}",
                payment.PaymentId, payment.ApplicationId, payment.Status);

            return MapToDto(payment);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PaymentResponseDto>> GetPaymentsByApplicationAsync(
            Guid applicationId)
        {
            var payments = await _db.Payments
                .Where(p => p.ApplicationId == applicationId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            return payment is null ? null : MapToDto(payment);
        }

        private static PaymentResponseDto MapToDto(Payment p) => new PaymentResponseDto
        {
            PaymentId       = p.PaymentId,
            ApplicationId   = p.ApplicationId,
            UserId          = p.UserId,
            AmountDisbursed = p.AmountDisbursed,
            Status          = p.Status,
            ReferenceNumber = p.ReferenceNumber,
            Message         = p.Message,
            CreatedAt       = p.CreatedAt,
            ProcessedAt     = p.ProcessedAt
        };
    }
}
