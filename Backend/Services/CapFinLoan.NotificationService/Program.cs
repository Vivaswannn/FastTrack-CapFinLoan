using CapFinLoan.NotificationService.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] " +
        "{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/notification-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Register RabbitMQ Consumer as Background Service ─────────
// AddSingleton so we can inject it into health endpoint
builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<RabbitMqConsumer>());

builder.Services.AddHostedService<OtpRabbitMqConsumer>();

// ── Health check endpoint ─────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// Health endpoint that reports RabbitMQ connection status
app.MapGet("/health", (RabbitMqConsumer consumer) => new
{
    status = consumer.IsConnected ? "Healthy" : "Degraded",
    service = "CapFinLoan.NotificationService",
    rabbitMq = new
    {
        connected = consumer.IsConnected,
        status = consumer.IsConnected ? "Connected" : "Disconnected"
    },
    timestamp = DateTime.UtcNow
});

app.MapHealthChecks("/healthz");

Log.Information(
    "CapFinLoan NotificationService starting on port 5005");

app.Run();
