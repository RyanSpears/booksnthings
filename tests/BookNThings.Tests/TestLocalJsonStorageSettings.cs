using BookNThings.Infrastructure.Configuration;

namespace BookNThings.Tests;

internal sealed class TestLocalJsonStorageSettings : ILocalJsonStorageSettings
{
    public TestLocalJsonStorageSettings(string dataDirectory)
    {
        DataDirectory = dataDirectory;
    }

    public string DataDirectory { get; private set; }

    public Task SetDataDirectoryAsync(string dataDirectory, CancellationToken cancellationToken)
    {
        DataDirectory = dataDirectory;
        return Task.CompletedTask;
    }

    public Task ResetToDefaultDataDirectoryAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
