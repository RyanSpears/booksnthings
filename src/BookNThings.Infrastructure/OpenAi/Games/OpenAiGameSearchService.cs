using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiGameSearchService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiGameSearchService> logger) : IGameSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenAiOptions _options = options.Value;

    public async Task<IReadOnlyList<Game>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("OpenAI game search rejected an empty query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("OpenAI API key is not configured.");
            throw new InvalidOperationException("OpenAI is not configured. Add OpenAI__ApiKey and try again.");
        }

        using var request = BuildRequest(query.Trim());

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var retryRequest = CloneRequest(request);
            using var response = await httpClient.SendAsync(retryRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return OpenAiGameResponseParser.Parse(ExtractStructuredOutput(content));
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout ||
                (int)response.StatusCode >= 500)
            {
                logger.LogWarning("OpenAI game request failed with status {StatusCode} on attempt {Attempt}. Body: {ResponseBody}", response.StatusCode, attempt, content);

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(350 * attempt), cancellationToken);
                    continue;
                }
            }

            var message = ExtractOpenAiErrorMessage(content);
            logger.LogError("OpenAI game request failed with status {StatusCode}. Body: {ResponseBody}", response.StatusCode, content);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "OpenAI request failed. Please try again shortly."
                : message);
        }

        throw new InvalidOperationException("OpenAI request failed after retries.");
    }

    private HttpRequestMessage BuildRequest(string query)
    {
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "results" },
            properties = new
            {
                results = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = 8,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "title", "publisher", "studio", "releasedDate", "genres", "rating", "developer" },
                        properties = new
                        {
                            title = new { type = "string" },
                            publisher = new { type = "string" },
                            studio = new { type = "string" },
                            releasedDate = new { type = "string", format = "date" },
                            genres = new { type = "array", items = new { type = "string" } },
                            rating = new
                            {
                                anyOf = new object[]
                                {
                                    new { type = "number" },
                                    new { type = "null" }
                                }
                            },
                            developer = new
                            {
                                anyOf = new object[]
                                {
                                    new { type = "string" },
                                    new { type = "null" }
                                }
                            }
                        }
                    }
                }
            }
        };

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4.1-mini" : _options.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "Return candidate computer games that match the user query. Use known release data where possible. Include a Metacritic rating when known; otherwise return null. Also include a few relevant genres."
                },
                new
                {
                    role = "user",
                    content = query
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "game_search_results",
                    strict = true,
                    schema
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        return request;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var body = source.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
        clone.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return clone;
    }

    private static string ExtractStructuredOutput(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString() ?? "";
        }

        if (root.TryGetProperty("output", out var output))
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content))
                {
                    continue;
                }

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? "";
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI response did not include structured output text.");
    }

    private static string? ExtractOpenAiErrorMessage(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore non-JSON error bodies and fall back to the generic message.
        }

        return null;
    }
}
