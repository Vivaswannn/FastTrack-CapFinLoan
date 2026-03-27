using System.Text;
using System.Text.Json;
using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.Services.Interfaces;
using CapFinLoan.SharedKernel.Enums;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.ApplicationService.Messaging
{
    /// <summary>
    /// Background service that consumes <see cref="PaymentProcessedEvent"/> from RabbitMQ.
    /// This is the final Saga step: if payment succeeded the loan is closed (Closed),
    /// if it failed the loan reverts to UnderReview so an admin can retry.
    /// </summary>
    public class PaymentProcessedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentProcessedConsumer> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;

        private const string ExchangeName = "capfinloan.events";
        private const string QueueName    = "payment.processed";
        private const string RoutingKey   = "payment.processed";
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        public PaymentProcessedConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentProcessedConsumer> logger,
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
                        "PaymentProcessedConsumer: connection lost — retrying in {Delay}s",
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
            _channel    = await _connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

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
                    var evt  = JsonSerializer.Deserialize<PaymentProcessedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evt is not null)
                        await HandleEventAsync(evt);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "PaymentProcessedConsumer: failed to process message — nacking");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "PaymentProcessedConsumer: connected and listening on queue '{Queue}'",
                QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleEventAsync(PaymentProcessedEvent evt)
        {
            _logger.LogInformation(
                "PaymentProcessedConsumer: received PaymentProcessedEvent " +
                "ApplicationId={AppId} Success={Success}",
                evt.ApplicationId, evt.Success);

            using var scope = _scopeFactory.CreateScope();
            var loanService = scope.ServiceProvider
                                   .GetRequiredService<ILoanApplicationService>();

            // Saga outcome: Success → Closed,  Failure → UnderReview
            var newStatus = evt.Success
                ? ApplicationStatus.Closed
                : ApplicationStatus.UnderReview;

            var remarks = evt.Success
                ? $"Loan disbursed successfully. Ref: {evt.Message}"
                : $"Payment failed — reverted to UnderReview. Reason: {evt.Message}";

            var dto = new UpdateApplicationStatusDto
            {
                NewStatus = newStatus,
                Remarks   = remarks
            };

            try
            {
                await loanService.UpdateStatusAsync(
                    evt.ApplicationId, dto, "PaymentService (Saga)");

                _logger.LogInformation(
                    "Saga completed: ApplicationId={AppId} → {Status}",
                    evt.ApplicationId, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Saga step FAILED for ApplicationId={AppId}: {Message}",
                    evt.ApplicationId, ex.Message);
                throw; // causes nack so message goes to dead-letter queue
            }
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
