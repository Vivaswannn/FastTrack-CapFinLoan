using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;

namespace CapFinLoan.PaymentService.Messaging
{
    /// <summary>
    /// Publishes <see cref="PaymentProcessedEvent"/> to RabbitMQ.
    /// Consumed by ApplicationService to close or revert the loan.
    /// </summary>
    public class PaymentEventPublisher : IAsyncDisposable, IDisposable
    {
        private readonly ILogger<PaymentEventPublisher> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private volatile bool _isConnected;
        private bool _disposed;

        private const string ExchangeName  = "capfinloan.events";
        private const string QueueName     = "payment.processed";
        private const string RoutingKey    = "payment.processed";

        public PaymentEventPublisher(
            ILogger<PaymentEventPublisher> logger,
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
                    "PaymentEventPublisher connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "PaymentEventPublisher: RabbitMQ unavailable — events will be skipped");
            }
        }

        /// <summary>
        /// Publishes a <see cref="PaymentProcessedEvent"/> to RabbitMQ.
        /// Fire-and-forget — does not throw if RabbitMQ is unavailable.
        /// </summary>
        public async Task PublishPaymentProcessedAsync(PaymentProcessedEvent evt)
        {
            if (!_isConnected || _channel is null)
            {
                _logger.LogWarning(
                    "PaymentEventPublisher: skipping event — not connected. ApplicationId={AppId}",
                    evt.ApplicationId);
                return;
            }

            try
            {
                var json  = JsonSerializer.Serialize(evt);
                var body  = Encoding.UTF8.GetBytes(json);
                var props = new BasicProperties { Persistent = true };

                await _channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body);

                _logger.LogInformation(
                    "PaymentProcessedEvent published: ApplicationId={AppId} Success={Success}",
                    evt.ApplicationId, evt.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish PaymentProcessedEvent for ApplicationId={AppId}",
                    evt.ApplicationId);
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
