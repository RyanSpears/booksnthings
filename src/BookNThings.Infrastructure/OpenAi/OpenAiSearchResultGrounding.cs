namespace BookNThings.Infrastructure.OpenAi;

public static class OpenAiSearchResultGrounding
{
    private static readonly HashSet<string> MediaWords =
    [
        "book", "books", "movie", "movies", "film", "films", "show", "shows", "series", "season",
        "game", "games", "rpg", "rpgs", "novel", "novels"
    ];

    private static readonly HashSet<string> BroadQueryWords =
    [
        "best", "top", "like", "similar", "recommend", "recommendations", "story", "stories",
        "driven", "style", "styles", "vibes", "prestige", "sci", "fi", "science", "fiction",
        "action", "adventure", "drama", "thriller", "comedy", "romance", "mystery", "horror",
        "fantasy", "strategy", "shooter", "platformer", "indie", "retro", "from", "with",
        "featuring", "watch", "read", "play"
    ];

    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "and", "as", "at", "be", "before", "behind", "between", "by", "for", "from",
        "in", "into", "is", "it", "of", "on", "or", "over", "the", "to", "under", "up", "via",
        "vs", "vs."
    ];

    public static IReadOnlyList<T> FilterSpecificMatches<T>(
        string query,
        IReadOnlyList<T> results,
        Func<T, string> titleSelector,
        Func<T, IEnumerable<string?>> searchableFieldSelector)
    {
        if (results.Count == 0 || !ShouldApplyStrictGrounding(query))
        {
            return results;
        }

        var queryParts = BuildQueryParts(query);
        if (queryParts.TitleTokens.Count == 0)
        {
            return results;
        }

        return results
            .Where(result => MatchesQuery(result, queryParts, titleSelector, searchableFieldSelector))
            .ToList();
    }

    private static bool ShouldApplyStrictGrounding(string query)
    {
        var normalizedQuery = Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return false;
        }

        if (HasExactPhraseConnector(normalizedQuery) || normalizedQuery.Contains('"'))
        {
            return true;
        }

        var tokens = Tokenize(normalizedQuery)
            .Where(token => !MediaWords.Contains(token) && !StopWords.Contains(token))
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        if (tokens.Any(token => BroadQueryWords.Contains(token)))
        {
            return false;
        }

        if (tokens.Any(token => token.Any(char.IsDigit)))
        {
            return true;
        }

        return tokens.Count <= 12;
    }

    private static QueryParts BuildQueryParts(string query)
    {
        var normalizedQuery = Normalize(query);
        var titleSegment = normalizedQuery;
        var entitySegment = "";

        foreach (var connector in new[] { " directed by ", " created by ", " written by ", " by " })
        {
            var index = normalizedQuery.IndexOf(connector, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                titleSegment = normalizedQuery[..index];
                entitySegment = normalizedQuery[(index + connector.Length)..];
                break;
            }
        }

        return new QueryParts(
            Tokenize(titleSegment)
                .Where(token => !MediaWords.Contains(token) && !BroadQueryWords.Contains(token) && !StopWords.Contains(token))
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            Tokenize(entitySegment)
                .Where(token => !MediaWords.Contains(token) && !BroadQueryWords.Contains(token) && !StopWords.Contains(token))
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private static bool MatchesQuery<T>(
        T result,
        QueryParts queryParts,
        Func<T, string> titleSelector,
        Func<T, IEnumerable<string?>> searchableFieldSelector)
    {
        var titleTokens = Tokenize(titleSelector(result))
            .Where(token => !MediaWords.Contains(token) && !BroadQueryWords.Contains(token) && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (queryParts.TitleTokens.Count > 0 && !queryParts.TitleTokens.All(titleTokens.Contains))
        {
            return false;
        }

        if (queryParts.EntityTokens.Count == 0)
        {
            return true;
        }

        var searchableTokens = Tokenize(string.Join(" ", searchableFieldSelector(result).Where(field => !string.IsNullOrWhiteSpace(field))))
            .Where(token => !MediaWords.Contains(token) && !BroadQueryWords.Contains(token) && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return queryParts.EntityTokens.All(searchableTokens.Contains);
    }

    private static bool HasExactPhraseConnector(string query) =>
        query.Contains(" by ", StringComparison.OrdinalIgnoreCase)
        || query.Contains(" directed by ", StringComparison.OrdinalIgnoreCase)
        || query.Contains(" created by ", StringComparison.OrdinalIgnoreCase)
        || query.Contains(" written by ", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"\bspiderman\b",
            "spider man",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static IEnumerable<string> Tokenize(string value) =>
        System.Text.RegularExpressions.Regex.Matches(Normalize(value), "[a-z0-9]+")
            .Select(match => match.Value)
            .Where(token => !string.IsNullOrWhiteSpace(token));

    private sealed record QueryParts(
        HashSet<string> TitleTokens,
        HashSet<string> EntityTokens);
}
