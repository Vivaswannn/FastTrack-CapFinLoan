using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.AdminService.Messaging
{
    /// <summary>
    /// Contract for publishing <see cref="LoanApprovedEvent"/> to RabbitMQ.
    /// Interface enables unit testing without a real RabbitMQ connection.
    /// </summary>
    public interface ILoanApprovedPublisher
    {
        /// <summary>
        /// Publishes a <see cref="LoanApprovedEvent"/> when a loan is approved.
        /// Consumed by PaymentService to initiate the Saga disbursement flow.
        /// </summary>
        Task PublishLoanApprovedAsync(LoanApprovedEvent approvedEvent);
    }
}
