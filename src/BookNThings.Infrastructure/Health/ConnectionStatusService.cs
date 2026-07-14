using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.Health;

public sealed class ConnectionStatusService(IOptions<OpenAiOptions> openAiOptions)
{
    public bool IsOpenAiConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.ApiKey);

    public bool IsOpenAiModelConfigured => !string.IsNullOrWhiteSpace(openAiOptions.Value.Model);
}
