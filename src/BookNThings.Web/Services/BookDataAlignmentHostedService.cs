using BookNThings.Application.Contracts;

namespace BookNThings.Web.Services;

public sealed class BookDataAlignmentHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookDataAlignmentHostedService> logger) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BookDataAlignmentHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IBookDataSynchronizer>();
            await synchronizer.AlignAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Book data alignment failed during startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
