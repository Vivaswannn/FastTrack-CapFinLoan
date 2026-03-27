using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.AuthService.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishOtpRequestedAsync(OtpRequestedEvent otpEvent);
    }
}
