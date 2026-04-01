using System.Text;
using System.Text.Json;
using CapFinLoan.SharedKernel.Events;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
                        await SendOtpEmailAsync(otpEvent);
                        _logger.LogInformation("OTP email sent to {Email}", otpEvent.Email);
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

        private async Task SendOtpEmailAsync(OtpRequestedEvent otpEvent)
        {
            var smtpHost = _configuration["Smtp:Host"]!;
            var smtpPort = int.Parse(_configuration["Smtp:Port"]!);
            var smtpUser = _configuration["Smtp:Username"]!;
            var smtpPass = _configuration["Smtp:Password"]!;
            var fromName = _configuration["Smtp:FromName"] ?? "CapFinLoan";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, smtpUser));
            message.To.Add(new MailboxAddress(otpEvent.FullName, otpEvent.Email));
            message.Subject = "Your CapFinLoan Login OTP";
            message.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e2e8f0;border-radius:12px'>
                  <h2 style='color:#0f172a;margin-bottom:8px'>CapFinLoan</h2>
                  <p style='color:#475569'>Hi {otpEvent.FullName}, here is your one-time login code:</p>
                  <div style='font-size:36px;font-weight:800;letter-spacing:12px;color:#0d9488;text-align:center;padding:24px 0'>{otpEvent.OtpCode}</div>
                  <p style='color:#94a3b8;font-size:13px'>This code expires in 5 minutes. Do not share it with anyone.</p>
                </div>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
