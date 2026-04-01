using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.DocumentService.Messaging
{
    /// <summary>
    /// Contract for publishing events to the message broker.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publishes a loan status changed event to RabbitMQ.
        /// </summary>
        Task PublishLoanStatusChangedAsync(LoanStatusChangedEvent statusEvent);
    }
}
