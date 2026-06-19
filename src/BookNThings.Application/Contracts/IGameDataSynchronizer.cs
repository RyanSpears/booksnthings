namespace BookNThings.Application.Contracts;

public interface IGameDataSynchronizer
{
    Task AlignAsync(CancellationToken cancellationToken);
}
