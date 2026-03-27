using System.Text;
using System.Text.Json;
using CapFinLoan.PaymentService.Services.Interfaces;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.PaymentService.Messaging
{
    /// <summary>
    /// Background service that consumes <see cref="LoanApprovedEvent"/> from RabbitMQ.
    /// Upon receiving an event it calls PaymentProcessingService to simulate disbursement
    /// and then publishes <see cref="PaymentProcessedEvent"/> — completing one step
    /// in the Saga choreography pattern.
    /// </summary>
    public class LoanApprovedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LoanApprovedConsumer> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;

        private const string ExchangeName = "capfinloan.events";
        private const string QueueName    = "loan.approved";
        private const string RoutingKey   = "loan.approved";
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        public LoanApprovedConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<LoanApprovedConsumer> logger,
            IConfiguration configuration)
        {
            _scopeFactory  = scopeFactory;
            _logger        = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "LoanApprovedConsumer: connection lost — retrying in {Delay}s",
                        RetryDelay.TotalSeconds);

                    await CleanupConnectionAsync();
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }

        private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName    = _configuration["RabbitMQ:Host"]     ?? "localhost",
                Port        = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName    = _configuration["RabbitMQ:Username"] ?? "guest",
                Password    = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel    = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var evt  = JsonSerializer.Deserialize<LoanApprovedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evt is not null)
                        await HandleEventAsync(evt);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "LoanApprovedConsumer: failed to process message — nacking");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "LoanApprovedConsumer: connected and listening on queue '{Queue}'",
                QueueName);

            // Keep alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleEventAsync(LoanApprovedEvent evt)
        {
            _logger.LogInformation(
                "LoanApprovedConsumer: received LoanApprovedEvent for ApplicationId={AppId}",
                evt.ApplicationId);

            using var scope          = _scopeFactory.CreateScope();
            var paymentService       = scope.ServiceProvider
                                           .GetRequiredService<IPaymentService>();
            var publisher            = scope.ServiceProvider
                                           .GetRequiredService<PaymentEventPublisher>();

            var paymentResult = await paymentService.ProcessPaymentAsync(evt);

            var processedEvent = new PaymentProcessedEvent
            {
                PaymentId       = paymentResult.PaymentId,
                ApplicationId   = evt.ApplicationId,
                UserId          = evt.UserId,
                Success         = paymentResult.Status == "Completed",
                AmountDisbursed = paymentResult.Status == "Completed"
                    ? paymentResult.AmountDisbursed
                    : null,
                Message   = paymentResult.Message ?? string.Empty,
                Timestamp = DateTime.UtcNow
            };

            await publisher.PublishPaymentProcessedAsync(processedEvent);
        }

        private async Task CleanupConnectionAsync()
        {
            try
            {
                if (_channel is not null) await _channel.DisposeAsync();
                if (_connection is not null) await _connection.DisposeAsync();
            }
            catch { /* ignore cleanup errors */ }
            finally
            {
                _channel    = null;
                _connection = null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await CleanupConnectionAsync();
        }
    }
}
