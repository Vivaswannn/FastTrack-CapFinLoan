using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.DocumentService.Messaging
{
    /// <summary>
    /// Publishes document-related events to RabbitMQ.
    /// Gracefully degrades if RabbitMQ is unavailable.
    /// </summary>
    public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private volatile bool _isConnected;
        private readonly SemaphoreSlim _reconnectLock = new(1, 1);
        private Timer? _reconnectTimer;
        private bool _disposed;

        private const string ExchangeName = "capfinloan.events";
        private const string RoutingKey  = "loan.status.changed";
        private static readonly TimeSpan BackgroundReconnectInterval = TimeSpan.FromSeconds(30);

        public RabbitMqPublisher(
            ILogger<RabbitMqPublisher> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            ConnectAsync().GetAwaiter().GetResult();
        }

        private ConnectionFactory BuildFactory() => new()
        {
            HostName    = _configuration["RabbitMQ:Host"]        ?? "localhost",
            Port        = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName    = _configuration["RabbitMQ:Username"]    ?? "guest",
            Password    = _configuration["RabbitMQ:Password"]    ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
        };

        private async Task ConnectAsync()
        {
            try
            {
                var factory = BuildFactory();
                _connection = await factory.CreateConnectionAsync();
                _channel    = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange:   ExchangeName,
                    type:       ExchangeType.Direct,
                    durable:    true,
                    autoDelete: false);

                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _isConnected = true;
                StopReconnectTimer();

                _logger.LogInformation(
                    "DocumentService RabbitMQ connection established. Exchange: {Exchange}",
                    ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "DocumentService RabbitMQ connection failed. " +
                    "Document rejection emails will not be sent.");
                _isConnected = false;
                StartReconnectTimer();
            }
        }

        private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
        {
            if (_disposed) return Task.CompletedTask;
            _isConnected = false;
            _logger.LogWarning("RabbitMQ connection lost: {Reason}", args.ReplyText);
            StartReconnectTimer();
            return Task.CompletedTask;
        }

        private void StartReconnectTimer()
        {
            if (_disposed || _reconnectTimer != null) return;
            _reconnectTimer = new Timer(
                async _ => { if (!_isConnected && !_disposed) await ConnectAsync(); },
                null, BackgroundReconnectInterval, BackgroundReconnectInterval);
        }

        private void StopReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        /// <inheritdoc/>
        public async Task PublishLoanStatusChangedAsync(LoanStatusChangedEvent statusEvent)
        {
            if (!_isConnected || _channel == null)
            {
                _logger.LogWarning(
                    "RabbitMQ not connected. Skipping notification for application {ApplicationId}",
                    statusEvent.ApplicationId);
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(statusEvent,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var properties = new BasicProperties
                {
                    Persistent  = true,
                    ContentType = "application/json",
                    MessageId   = statusEvent.EventId.ToString(),
                    Timestamp   = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                };

                await _channel.BasicPublishAsync(
                    exchange:        ExchangeName,
                    routingKey:      RoutingKey,
                    mandatory:       false,
                    basicProperties: properties,
                    body:            Encoding.UTF8.GetBytes(json));

                _logger.LogInformation(
                    "Event published: {NewStatus} for AppId={ApplicationId} To={Email}",
                    statusEvent.NewStatus, statusEvent.ApplicationId, statusEvent.ApplicantEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event for application {ApplicationId}",
                    statusEvent.ApplicationId);
                _isConnected = false;
            }
        }

        private async Task DisposeConnectionAsync()
        {
            try { if (_connection != null) _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync; } catch { }
            try { if (_channel    != null) await _channel.CloseAsync();    } catch { }
            try { if (_connection != null) await _connection.CloseAsync(); } catch { }
            _channel?.Dispose();
            _connection?.Dispose();
            _channel    = null;
            _connection = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            StopReconnectTimer();
            await DisposeConnectionAsync();
            _reconnectLock.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopReconnectTimer();
            _channel?.Dispose();
            _connection?.Dispose();
            _reconnectLock.Dispose();
        }
    }
}
