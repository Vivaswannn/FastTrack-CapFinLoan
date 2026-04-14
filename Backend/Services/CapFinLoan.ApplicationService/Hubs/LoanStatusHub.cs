using Microsoft.AspNetCore.SignalR;

namespace CapFinLoan.ApplicationService.Hubs
{
    /// <summary>
    /// SignalR hub for real-time loan status push notifications.
    /// Clients join a group named after their ApplicationId and receive
    /// StatusUpdated events the moment an admin changes a loan status.
    /// </summary>
    public class LoanStatusHub : Hub
    {
        private readonly ILogger<LoanStatusHub> _logger;

        public LoanStatusHub(ILogger<LoanStatusHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called by the client to subscribe to updates for a specific application.
        /// </summary>
        public async Task JoinApplicationGroup(string applicationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, applicationId);
            _logger.LogInformation(
                "Client {ConnectionId} joined group {ApplicationId}",
                Context.ConnectionId, applicationId);
        }

        /// <summary>
        /// Called by the client to stop receiving updates for a specific application.
        /// </summary>
        public async Task LeaveApplicationGroup(string applicationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, applicationId);
        }
    }
}
