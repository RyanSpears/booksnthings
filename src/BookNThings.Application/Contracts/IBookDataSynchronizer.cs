namespace BookNThings.Application.Contracts;

public interface IBookDataSynchronizer
{
    Task AlignAsync(CancellationToken cancellationToken);
}
