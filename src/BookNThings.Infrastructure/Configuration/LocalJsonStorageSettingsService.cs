using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalJsonStorageSettingsService : ILocalJsonStorageSettings
{
    private static readonly string[] StorageFileNames = ["books.json", "games.json", "movies.json", "show.json"];
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<LocalJsonStorageSettingsService> _logger;
    private readonly string _settingsFilePath;
    private readonly string _defaultDataDirectory;
    private readonly object _sync = new();
    private string _dataDirectory;

    public LocalJsonStorageSettingsService(
        ILogger<LocalJsonStorageSettingsService> logger,
        string? settingsFilePath = null,
        string? defaultDataDirectory = null)
    {
        _logger = logger;
        _defaultDataDirectory = string.IsNullOrWhiteSpace(defaultDataDirectory)
            ? Path.Combine(Directory.GetCurrentDirectory(), "Data")
            : NormalizeDirectory(defaultDataDirectory);
        _settingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BookNThings",
                "local-json-storage-settings.json")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(settingsFilePath));
        _dataDirectory = LoadDataDirectory();
        EnsureStorageFilesExist(_dataDirectory);
    }

    public string DataDirectory
    {
        get
        {
            lock (_sync)
            {
                return _dataDirectory;
            }
        }
    }

    public async Task SetDataDirectoryAsync(string dataDirectory, CancellationToken cancellationToken)
    {
        var normalizedDirectory = NormalizeDirectory(dataDirectory);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await PersistDataDirectoryAsync(normalizedDirectory, persistSetting: true, cancellationToken);
            EnsureStorageFilesExist(normalizedDirectory);

            lock (_sync)
            {
                _dataDirectory = normalizedDirectory;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetToDefaultDataDirectoryAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                File.Delete(_settingsFilePath);
            }

            await PersistDataDirectoryAsync(_defaultDataDirectory, persistSetting: false, cancellationToken);
            EnsureStorageFilesExist(_defaultDataDirectory);

            lock (_sync)
            {
                _dataDirectory = _defaultDataDirectory;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private string LoadDataDirectory()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return NormalizeDirectory(_defaultDataDirectory);
            }

            using var stream = File.OpenRead(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<LocalJsonStorageSettings>(stream, SerializerOptions);
            return NormalizeDirectory(settings?.DataDirectory ?? _defaultDataDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load the saved JSON storage settings. Falling back to the default Data folder.");
            return NormalizeDirectory(_defaultDataDirectory);
        }
    }

    private void EnsureStorageFilesExist(string dataDirectory)
    {
        try
        {
            Directory.CreateDirectory(dataDirectory);

            foreach (var fileName in StorageFileNames)
            {
                var filePath = Path.Combine(dataDirectory, fileName);
                if (File.Exists(filePath))
                {
                    continue;
                }

                File.WriteAllText(filePath, "[]");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed missing JSON storage files in {DataDirectory}.", dataDirectory);
        }
    }

    private async Task PersistDataDirectoryAsync(string dataDirectory, bool persistSetting, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(dataDirectory);

        if (!persistSetting)
        {
            return;
        }

        var settings = new LocalJsonStorageSettings
        {
            DataDirectory = dataDirectory
        };

        var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        var tempFile = $"{_settingsFilePath}.tmp";
        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
        }

        File.Copy(tempFile, _settingsFilePath, true);
        File.Delete(tempFile);
    }

    private static string NormalizeDirectory(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dataDirectory));
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
