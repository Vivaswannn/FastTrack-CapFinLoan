using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using CapFinLoan.NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.NotificationService.Messaging
{
    /// <summary>
    /// Background service that continuously listens to RabbitMQ
    /// for LoanStatusChangedEvent messages.
    /// Runs for the entire lifetime of the application.
    /// Uses IHostedService so it starts with the app automatically.
    /// Messages that fail processing 3 times are routed to a
    /// dead letter queue instead of being requeued indefinitely.
    /// </summary>
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private volatile bool _isConnected;

        private const string ExchangeName = "capfinloan.events";
        private const string QueueName = "loan.status.changed";
        private const string RoutingKey = "loan.status.changed";
        private const string DeadLetterExchange = "capfinloan.events.dlx";
        private const string DeadLetterQueue = "loan.status.changed.dlq";
        private const string DeadLetterRoutingKey = "loan.status.changed.dead";
        private const int MaxRetryCount = 3;

        /// <summary>
        /// Gets whether the consumer is currently connected to RabbitMQ.
        /// </summary>
        public bool IsConnected => _isConnected;

        public RabbitMqConsumer(
            ILogger<RabbitMqConsumer> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "NotificationService starting. " +
                "Connecting to RabbitMQ...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown — do not retry
                    break;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    _logger.LogError(ex,
                        "RabbitMQ consumer error. " +
                        "Retrying in 10 seconds...");

                    await Task.Delay(
                        TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ConnectAndConsumeAsync(
            CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration[
                    "RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration[
                    "RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration[
                    "RabbitMQ:Username"] ?? "guest",
                Password = _configuration[
                    "RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration[
                    "RabbitMQ:VirtualHost"] ?? "/",
            };

            _connection = await factory.CreateConnectionAsync(
                stoppingToken);
            _channel = await _connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

            // ── Dead Letter Exchange + Queue ──────────────────────
            // Messages that fail MaxRetryCount times go here
            await _channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: DeadLetterQueue,
                exchange: DeadLetterExchange,
                routingKey: DeadLetterRoutingKey,
                cancellationToken: stoppingToken);

            // ── Main Exchange + Queue with DLX arguments ──────────
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", DeadLetterRoutingKey }
            };

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey,
                cancellationToken: stoppingToken);

            // Process one message at a time
            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            _isConnected = true;

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogInformation(
                        "Message received from RabbitMQ. " +
                        "Processing...");

                    var statusEvent = JsonSerializer.Deserialize
                        <LoanStatusChangedEvent>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (statusEvent != null)
                    {
                        await ProcessEventAsync(statusEvent);
                    }

                    // Acknowledge message — remove from queue
                    await _channel.BasicAckAsync(
                        ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    var retryCount = GetRetryCount(ea.BasicProperties);

                    _logger.LogError(ex,
                        "Error processing message (attempt {Attempt}/{Max}): {Json}",
                        retryCount + 1, MaxRetryCount, json);

                    if (retryCount >= MaxRetryCount - 1)
                    {
                        // Max retries exceeded — reject without requeue
                        // RabbitMQ routes to DLX automatically
                        _logger.LogWarning(
                            "Message exceeded {Max} retries. " +
                            "Routing to dead letter queue: {DLQ}",
                            MaxRetryCount, DeadLetterQueue);

                        await _channel.BasicNackAsync(
                            ea.DeliveryTag, false, requeue: false);
                    }
                    else
                    {
                        // Requeue for retry
                        await _channel.BasicNackAsync(
                            ea.DeliveryTag, false, requeue: true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "RabbitMQ consumer started. " +
                "Listening on queue: {Queue}, DLQ: {DLQ}",
                QueueName, DeadLetterQueue);

            // Keep running until cancelled
            await Task.Delay(
                Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// Extracts the retry count from the x-death header.
        /// RabbitMQ increments x-death each time a message is
        /// dead-lettered and re-delivered.
        /// </summary>
        private static int GetRetryCount(IReadOnlyBasicProperties properties)
        {
            if (properties.Headers == null)
                return 0;

            if (!properties.Headers.TryGetValue("x-death", out var deathObj))
                return 0;

            if (deathObj is List<object> deathList && deathList.Count > 0)
            {
                if (deathList[0] is Dictionary<string, object> firstDeath &&
                    firstDeath.TryGetValue("count", out var countObj))
                {
                    return Convert.ToInt32(countObj);
                }
            }

            return 0;
        }

        private async Task ProcessEventAsync(
            LoanStatusChangedEvent evt)
        {
            _logger.LogInformation(
                "Processing LoanStatusChangedEvent: " +
                "AppId={ApplicationId} " +
                "{OldStatus} -> {NewStatus} " +
                "Applicant={Email}",
                evt.ApplicationId,
                evt.OldStatus,
                evt.NewStatus,
                evt.ApplicantEmail);

            var notification = NotificationBuilder.Build(evt);

            if (notification == null)
            {
                _logger.LogInformation(
                    "No notification needed for " +
                    "status {NewStatus}", evt.NewStatus);
                return;
            }

            // In production: send real email via MailKit/SendGrid
            // For training: log the notification details
            _logger.LogInformation(
                "NOTIFICATION SENT\n" +
                "  To:      {Email}\n" +
                "  Name:    {Name}\n" +
                "  Subject: {Subject}\n" +
                "  AppId:   {AppId}\n" +
                "  Status:  {OldStatus} -> {NewStatus}",
                notification.RecipientEmail,
                notification.RecipientName,
                notification.Subject,
                evt.ApplicationId,
                evt.OldStatus,
                evt.NewStatus);

            _logger.LogDebug(
                "Notification body:\n{Body}",
                notification.Body);

            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            _isConnected = false;
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
