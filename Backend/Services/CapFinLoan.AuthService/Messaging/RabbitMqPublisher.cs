using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.AuthService.Messaging
{
    public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private volatile bool _isConnected;
        private bool _disposed;

        private const string ExchangeName = "capfinloan.events";
        private const string RoutingKey = "auth.otp.requested";

        public RabbitMqPublisher(
            ILogger<RabbitMqPublisher> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            ConnectAsync().GetAwaiter().GetResult();
        }

        private ConnectionFactory BuildFactory()
        {
            return new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            };
        }

        private async Task ConnectAsync()
        {
            try
            {
                var factory = BuildFactory();
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                _isConnected = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection failed.");
                _isConnected = false;
            }
        }

        public async Task PublishOtpRequestedAsync(OtpRequestedEvent otpEvent)
        {
            if (!_isConnected || _channel == null)
            {
                await ConnectAsync();
                if (!_isConnected) return;
            }

            try
            {
                var json = JsonSerializer.Serialize(otpEvent,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var body = Encoding.UTF8.GetBytes(json);
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                };

                await _channel!.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation($"Event published: OtpRequested To={otpEvent.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OTP event.");
                _isConnected = false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
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
