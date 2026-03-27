using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.ApplicationService.Messaging
{
    /// <summary>
    /// Contract for publishing events to message broker.
    /// Using an interface enables unit testing without
    /// a real RabbitMQ connection.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publishes a loan status changed event to RabbitMQ.
        /// Fire-and-forget — does not block the caller.
        /// </summary>
        Task PublishLoanStatusChangedAsync(
            LoanStatusChangedEvent statusEvent);
    }
}
