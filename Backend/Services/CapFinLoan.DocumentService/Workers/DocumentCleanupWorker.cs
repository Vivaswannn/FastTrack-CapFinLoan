using CapFinLoan.DocumentService.Data;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.DocumentService.Workers;

public class DocumentCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentCleanupWorker> _logger;
    private readonly IWebHostEnvironment _env;

    public DocumentCleanupWorker(
        IServiceProvider serviceProvider,
        ILogger<DocumentCleanupWorker> logger,
        IWebHostEnvironment env)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("DocumentCleanupWorker running at: {time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();

                // Get all documents marked as replaced out of the DB
                var replacedDocs = await dbContext.Documents
                    .Where(d => d.IsReplaced)
                    .ToListAsync(stoppingToken);

                if (replacedDocs.Count != 0)
                {
                    _logger.LogInformation("Found {Count} replaced documents to clean up.", replacedDocs.Count);

                    var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");

                    foreach (var doc in replacedDocs)
                    {
                        if (!string.IsNullOrEmpty(doc.FilePath))
                        {
                            var filename = Path.GetFileName(doc.FilePath);
                            var physicalPath = Path.Combine(uploadsFolder, filename);

                            if (File.Exists(physicalPath))
                            {
                                File.Delete(physicalPath);
                                _logger.LogInformation("Deleted physical file: {PhysicalPath}", physicalPath);
                            }
                        }

                        // Remove the record from the database so it stops taking up space
                        dbContext.Documents.Remove(doc);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleanup complete. Removed records from database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during document cleanup.");
            }

            // Polling interval set to 10 seconds for demonstration and testing purposes.
            // In a real production system, this would be set to TimeSpan.FromHours(24).
            await Task.Delay(10000, stoppingToken);
        }
    }
}
