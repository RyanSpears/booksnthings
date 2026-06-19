using BookNThings.Application.Contracts;

namespace BookNThings.Web.Services;

public sealed class GameDataAlignmentHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<GameDataAlignmentHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<GameDataAlignmentHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Yield();
            using var scope = _scopeFactory.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IGameDataSynchronizer>();
            await synchronizer.AlignAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Game data alignment failed during startup.");
        }
    }
}
