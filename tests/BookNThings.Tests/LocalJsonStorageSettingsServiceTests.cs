using BookNThings.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookNThings.Tests;

public class LocalJsonStorageSettingsServiceTests
{
    [Fact]
    public async Task SetDataDirectoryAsync_Should_Seed_Missing_Json_Files()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "BookNThingsTests", Guid.NewGuid().ToString("N"));
        var defaultDirectory = Path.Combine(rootDirectory, "default");
        var settingsFilePath = Path.Combine(rootDirectory, "settings", "local-json-storage-settings.json");
        var targetDirectory = Path.Combine(rootDirectory, "chosen");

        try
        {
            var service = new LocalJsonStorageSettingsService(
                NullLogger<LocalJsonStorageSettingsService>.Instance,
                settingsFilePath,
                defaultDirectory);

            await service.SetDataDirectoryAsync(targetDirectory, CancellationToken.None);

            service.DataDirectory.Should().Be(Path.GetFullPath(targetDirectory));

            foreach (var fileName in new[] { "books.json", "games.json", "movies.json", "show.json" })
            {
                var filePath = Path.Combine(targetDirectory, fileName);
                File.Exists(filePath).Should().BeTrue();
                var contents = await File.ReadAllTextAsync(filePath);
                contents.Should().Be("[]");
            }
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, true);
            }
        }
    }

    [Fact]
    public async Task ResetToDefaultDataDirectoryAsync_Should_Clear_Custom_Settings_And_Seed_Default_Files()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "BookNThingsTests", Guid.NewGuid().ToString("N"));
        var defaultDirectory = Path.Combine(rootDirectory, "default");
        var settingsFilePath = Path.Combine(rootDirectory, "settings", "local-json-storage-settings.json");
        var targetDirectory = Path.Combine(rootDirectory, "chosen");

        try
        {
            var service = new LocalJsonStorageSettingsService(
                NullLogger<LocalJsonStorageSettingsService>.Instance,
                settingsFilePath,
                defaultDirectory);

            await service.SetDataDirectoryAsync(targetDirectory, CancellationToken.None);
            await service.ResetToDefaultDataDirectoryAsync(CancellationToken.None);

            service.DataDirectory.Should().Be(Path.GetFullPath(defaultDirectory));
            File.Exists(settingsFilePath).Should().BeFalse();

            foreach (var fileName in new[] { "books.json", "games.json", "movies.json", "show.json" })
            {
                var filePath = Path.Combine(defaultDirectory, fileName);
                File.Exists(filePath).Should().BeTrue();
                var contents = await File.ReadAllTextAsync(filePath);
                contents.Should().Be("[]");
            }
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, true);
            }
        }
    }
}
