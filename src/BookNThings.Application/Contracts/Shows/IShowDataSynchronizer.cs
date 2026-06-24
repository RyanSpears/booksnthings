namespace BookNThings.Application.Contracts;

public interface IShowDataSynchronizer
{
    Task AlignAsync(CancellationToken cancellationToken);
}
