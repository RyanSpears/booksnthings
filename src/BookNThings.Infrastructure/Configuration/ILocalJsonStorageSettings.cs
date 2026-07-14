namespace BookNThings.Infrastructure.Configuration;

public interface ILocalJsonStorageSettings
{
    string DataDirectory { get; }

    Task SetDataDirectoryAsync(string dataDirectory, CancellationToken cancellationToken);

    Task ResetToDefaultDataDirectoryAsync(CancellationToken cancellationToken);
}
