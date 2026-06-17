using BookNThings.Application.Contracts;

namespace BookNThings.Web.Services;

public sealed class BookDataAlignmentHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookDataAlignmentHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BookDataAlignmentHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Yield();
            using var scope = _scopeFactory.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IBookDataSynchronizer>();
            await synchronizer.AlignAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Book data alignment failed during startup.");
        }
    }
}
