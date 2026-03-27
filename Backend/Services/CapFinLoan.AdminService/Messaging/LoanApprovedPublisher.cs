using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;

namespace CapFinLoan.AdminService.Messaging
{
    /// <summary>
    /// Publishes <see cref="LoanApprovedEvent"/> to RabbitMQ when a loan is approved.
    /// PaymentService consumes this event to kick off the Saga disbursement flow.
    /// Gracefully degrades if RabbitMQ is unavailable.
    /// </summary>
    public class LoanApprovedPublisher : ILoanApprovedPublisher, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<LoanApprovedPublisher> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private volatile bool _isConnected;
        private bool _disposed;

        private const string ExchangeName = "capfinloan.events";
        private const string QueueName    = "loan.approved";
        private const string RoutingKey   = "loan.approved";

        public LoanApprovedPublisher(
            ILogger<LoanApprovedPublisher> logger,
            IConfiguration configuration)
        {
            _logger        = logger;
            _configuration = configuration;
            ConnectAsync().GetAwaiter().GetResult();
        }

        private async Task ConnectAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName    = _configuration["RabbitMQ:Host"]     ?? "localhost",
                    Port        = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName    = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password    = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel    = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                await _channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey);

                _isConnected = true;
                _logger.LogInformation(
                    "LoanApprovedPublisher connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "LoanApprovedPublisher: RabbitMQ unavailable — events will be skipped");
            }
        }

        /// <inheritdoc/>
        public async Task PublishLoanApprovedAsync(LoanApprovedEvent approvedEvent)
        {
            if (!_isConnected || _channel is null)
            {
                _logger.LogWarning(
                    "LoanApprovedPublisher: skipping event — not connected. ApplicationId={AppId}",
                    approvedEvent.ApplicationId);
                return;
            }

            try
            {
                var json  = JsonSerializer.Serialize(approvedEvent);
                var body  = Encoding.UTF8.GetBytes(json);
                var props = new BasicProperties { Persistent = true };

                await _channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body);

                _logger.LogInformation(
                    "LoanApprovedEvent published for ApplicationId={AppId} Amount={Amount}",
                    approvedEvent.ApplicationId, approvedEvent.LoanAmountApproved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish LoanApprovedEvent for ApplicationId={AppId}",
                    approvedEvent.ApplicationId);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
