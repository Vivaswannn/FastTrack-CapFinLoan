using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.ApplicationService.Messaging
{
    /// <summary>
    /// Publishes events to RabbitMQ message broker.
    /// Uses direct exchange with durable queue for reliability.
    /// Automatically reconnects if RabbitMQ goes down and comes back
    /// via connection shutdown event handler + background retry timer.
    /// If RabbitMQ is unavailable, logs warning and continues
    /// so the application works even without messaging.
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
        private const string QueueName = "loan.status.changed";
        private const string RoutingKey = "loan.status.changed";
        private const int MaxReconnectAttempts = 3;
        private static readonly TimeSpan BackgroundReconnectInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether the publisher is currently connected to RabbitMQ.
        /// </summary>
        public bool IsConnected => _isConnected;
        
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

                // Declare durable exchange
                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                // NOTE: Queue is declared by the consumer (NotificationService) with DLX args.
                // The publisher only needs the exchange to route messages.

                // Subscribe to connection shutdown for auto-reconnect
                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;

                _isConnected = true;

                // Stop background reconnect timer if running
                StopReconnectTimer();

                _logger.LogInformation(
                    "RabbitMQ connection established. " +
                    "Exchange: {Exchange}, Queue: {Queue}",
                    ExchangeName, QueueName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ connection failed. " +
                    "Events will not be published. " +
                    "Application continues to work normally.");
                _isConnected = false;

                // Start background timer to keep retrying
                StartReconnectTimer();
            }
        }

        /// <summary>
        /// Handles RabbitMQ connection shutdown events.
        /// Marks connection as lost and starts background reconnection.
        /// </summary>
        private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
        {
            if (_disposed) return Task.CompletedTask;

            _isConnected = false;
            _logger.LogWarning(
                "RabbitMQ connection lost. Reason: {Reason}. " +
                "Starting background reconnection...",
                args.ReplyText);

            StartReconnectTimer();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts a background timer that attempts reconnection periodically.
        /// </summary>
        private void StartReconnectTimer()
        {
            if (_disposed || _reconnectTimer != null) return;

            _reconnectTimer = new Timer(
                async _ => await BackgroundReconnectAsync(),
                null,
                BackgroundReconnectInterval,
                BackgroundReconnectInterval);

            _logger.LogInformation(
                "Background reconnect timer started. " +
                "Will retry every {Seconds}s",
                BackgroundReconnectInterval.TotalSeconds);
        }

        /// <summary>
        /// Stops the background reconnection timer.
        /// </summary>
        private void StopReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        /// <summary>
        /// Background reconnection callback invoked by the timer.
        /// </summary>
        private async Task BackgroundReconnectAsync()
        {
            if (_isConnected || _disposed) return;

            _logger.LogInformation("Background reconnect attempt...");
            await TryReconnectAsync();
        }

        /// <summary>
        /// Attempts to reconnect to RabbitMQ if the connection was lost.
        /// Uses a semaphore to prevent concurrent reconnection attempts.
        /// </summary>
        private async Task<bool> TryReconnectAsync()
        {
            if (_isConnected && _channel is not null)
                return true;

            if (!await _reconnectLock.WaitAsync(0))
                return false; // Another thread is already reconnecting

            try
            {
                // Dispose old resources
                await DisposeConnectionAsync();

                for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
                {
                    try
                    {
                        _logger.LogInformation(
                            "RabbitMQ reconnection attempt {Attempt}/{Max}...",
                            attempt, MaxReconnectAttempts);

                        await ConnectAsync();

                        if (_isConnected)
                        {
                            _logger.LogInformation(
                                "RabbitMQ reconnection successful on attempt {Attempt}",
                                attempt);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "RabbitMQ reconnection attempt {Attempt} failed",
                            attempt);
                    }

                    if (attempt < MaxReconnectAttempts)
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }

                _logger.LogWarning(
                    "RabbitMQ reconnection failed after {Max} attempts. " +
                    "Will retry via background timer.",
                    MaxReconnectAttempts);
                return false;
            }
            finally
            {
                _reconnectLock.Release();
            }
        }

        /// <summary>
        /// Safely disposes the existing connection and channel.
        /// </summary>
        private async Task DisposeConnectionAsync()
        {
            try
            {
                if (_connection != null)
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
            }
            catch { /* ignore */ }

            try
            {
                if (_channel != null)
                    await _channel.CloseAsync();
            }
            catch { /* ignore */ }

            try
            {
                if (_connection != null)
                    await _connection.CloseAsync();
            }
            catch { /* ignore */ }

            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }

        /// <inheritdoc/>
        public async Task PublishLoanStatusChangedAsync(
            LoanStatusChangedEvent statusEvent)
        {
            // Attempt reconnection if disconnected
            if (!_isConnected || _channel == null)
            {
                var reconnected = await TryReconnectAsync();
                if (!reconnected)
                {
                    _logger.LogWarning(
                        "RabbitMQ not connected. Skipping event " +
                        "for application {ApplicationId}",
                        statusEvent.ApplicationId);
                    return;
                }
            }

            try
            {
                var json = JsonSerializer.Serialize(statusEvent,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });

                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = statusEvent.EventId.ToString(),
                    Timestamp = new AmqpTimestamp(
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                };

                await _channel!.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation(
                    "Event published: LoanStatusChanged " +
                    "AppId={ApplicationId} " +
                    "{OldStatus} -> {NewStatus} " +
                    "To={Email}",
                    statusEvent.ApplicationId,
                    statusEvent.OldStatus,
                    statusEvent.NewStatus,
                    statusEvent.ApplicantEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event for " +
                    "application {ApplicationId}. " +
                    "Marking connection as lost for reconnection.",
                    statusEvent.ApplicationId);

                // Mark disconnected so next call triggers reconnect
                _isConnected = false;

                // Do NOT rethrow — messaging failure should not
                // break the main application flow
            }
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
