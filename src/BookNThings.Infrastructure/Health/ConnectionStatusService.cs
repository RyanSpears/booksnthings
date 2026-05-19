using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Health;

public sealed class ConnectionStatusService(IOptions<MongoDbOptions> mongoOptions, IOptions<OpenAiOptions> openAiOptions)
{
    public bool IsOpenAiConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.ApiKey);

    public bool IsOpenAiModelConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.Model);

    public bool IsMongoConfigured => !string.IsNullOrWhiteSpace(mongoOptions.Value.ConnectionString);

    public async Task<bool> CanConnectToMongoAsync(CancellationToken cancellationToken)
    {
        if (!IsMongoConfigured)
        {
            return false;
        }

        var client = new MongoClient(mongoOptions.Value.ConnectionString);
        var databaseName = string.IsNullOrWhiteSpace(mongoOptions.Value.DatabaseName)
            ? "booknthings"
            : mongoOptions.Value.DatabaseName;

        var command = new BsonDocument("ping", 1);
        await client.GetDatabase(databaseName).RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        return true;
    }
}
