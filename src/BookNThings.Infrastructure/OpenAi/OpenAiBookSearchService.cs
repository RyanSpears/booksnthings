using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiBookSearchService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiBookSearchService> logger) : IBookSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenAiOptions _options = options.Value;

    public async Task<IReadOnlyList<Book>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("OpenAI search rejected an empty query.");
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
                return OpenAiBookResponseParser.Parse(ExtractStructuredOutput(content));
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout ||
                (int)response.StatusCode >= 500)
            {
                logger.LogWarning("OpenAI request failed with status {StatusCode} on attempt {Attempt}.", response.StatusCode, attempt);

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(350 * attempt), cancellationToken);
                    continue;
                }
            }

            logger.LogError("OpenAI request failed with status {StatusCode}.", response.StatusCode);
            throw new InvalidOperationException("OpenAI request failed. Please try again shortly.");
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
                        required = new[] { "title", "description", "pages", "datePublished", "genres", "author" },
                        properties = new
                        {
                            title = new { type = "string" },
                            description = new { type = "string" },
                            pages = new { type = "integer", minimum = 0 },
                            datePublished = new { type = "string", format = "date" },
                            genres = new { type = "array", items = new { type = "string" } },
                            author = new { type = "string" }
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
                    content = "Return candidate books that match the user query. Use known bibliographic data where possible."
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
                    name = "book_search_results",
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
}
