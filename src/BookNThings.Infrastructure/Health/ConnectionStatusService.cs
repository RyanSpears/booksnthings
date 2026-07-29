using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.Health;

public sealed class ConnectionStatusService(
    IOptions<OpenAiOptions> openAiOptions,
    IOptions<IgdbOptions> igdbOptions)
{
    public bool IsOpenAiConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.ApiKey);

    public bool IsOpenAiModelConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.Model);

    public bool IsIgdbClientIdConfigured => !string.IsNullOrWhiteSpace(igdbOptions.Value.ClientId);

    public bool IsIgdbClientSecretConfigured => !string.IsNullOrWhiteSpace(igdbOptions.Value.ClientSecret);
}
