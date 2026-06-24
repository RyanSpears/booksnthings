using BookNThings.Application.Contracts;

namespace BookNThings.Web.Services;

public sealed class ShowDataAlignmentHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ShowDataAlignmentHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ShowDataAlignmentHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Yield();
            using var scope = _scopeFactory.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IShowDataSynchronizer>();
            await synchronizer.AlignAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Show data alignment failed during startup.");
        }
    }
}
