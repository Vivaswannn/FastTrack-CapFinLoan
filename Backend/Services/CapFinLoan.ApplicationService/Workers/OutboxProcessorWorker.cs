using System.Text.Json;
using CapFinLoan.ApplicationService.Data;
using CapFinLoan.ApplicationService.Messaging;
using CapFinLoan.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CapFinLoan.ApplicationService.Workers;

/// <summary>
/// Background worker that polls the OutboxMessages table and publishes
/// pending events to RabbitMQ via IMessagePublisher.
/// </summary>
public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorWorker> _logger;

    public OutboxProcessorWorker(IServiceProvider serviceProvider, ILogger<OutboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                // Fetch unprocessed messages
                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        if (message.EventType == nameof(LoanStatusChangedEvent))
                        {
                            var evt = JsonSerializer.Deserialize<LoanStatusChangedEvent>(message.Payload);
                            if (evt != null)
                            {
                                await publisher.PublishLoanStatusChangedAsync(evt);
                            }
                        }

                        message.ProcessedAt = DateTime.UtcNow;
                        _logger.LogInformation("Successfully processed outbox message {Id}", message.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                        message.Error = ex.Message;
                    }
                }

                if (messages.Count != 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing OutboxProcessorWorker polling loop");
            }

            // Poll every 10 seconds
            await Task.Delay(10000, stoppingToken);
        }
    }
}
