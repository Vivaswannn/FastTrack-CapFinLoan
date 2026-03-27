using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CapFinLoan.NotificationService.Messaging
{
    public class OtpRabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<OtpRabbitMqConsumer> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private const string ExchangeName = "capfinloan.events";
        private const string QueueName = "auth.otp.requested";
        private const string RoutingKey = "auth.otp.requested";

        public OtpRabbitMqConsumer(
            ILogger<OtpRabbitMqConsumer> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OTP Notification Consumer starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ OTP consumer error. Retrying in 10s...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, true, false, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(QueueName, true, false, false, null, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);
            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var otpEvent = JsonSerializer.Deserialize<OtpRequestedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (otpEvent != null)
                    {
                        _logger.LogInformation(
                            "---------------------------------------\n" +
                            "OTP NOTIFICATION SENT\n" +
                            "  To:    {Email}\n" +
                            "  Name:  {Name}\n" +
                            "  OTP:   {OtpCode}\n" +
                            "---------------------------------------",
                            otpEvent.Email, otpEvent.FullName, otpEvent.OtpCode);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing OTP message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(QueueName, false, consumer, stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
