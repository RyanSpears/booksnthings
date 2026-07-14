# BookNThings

BookNThings is a local-deployable .NET 10 Blazor Web App for finding books and games with natural language, validating OpenAI structured JSON outputs, saving selected records to local JSON files, and browsing saved media.

## Architecture

- `src/BookNThings.Web`: Blazor Web App UI, routing, forms, configuration bootstrap, dependency injection, and user-friendly error handling.
- `src/BookNThings.Application`: service contracts, domain model, validation, and orchestration logic without external integration details.
- `src/BookNThings.Infrastructure`: local JSON repositories, OpenAI structured output integration, configuration models, and app settings helpers.
- `tests/BookNThings.Tests`: xUnit tests covering models, validation, application services, repository contracts, and OpenAI parsing.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- OpenAI API key

The app stores books in `books.json`, games in `games.json`, and TV shows in `show.json` inside the configured local JSON storage folder.

## OpenAI Setup

Create an OpenAI API key and configure:

```powershell
$env:OpenAI__ApiKey="sk-..."
$env:OpenAI__Model="gpt-4.1-mini"
```

The OpenAI integration uses the Responses API with strict JSON schema structured outputs.

## Environment Variables

Local development:

```text
OpenAI__ApiKey
OpenAI__Model
```

## User Secrets

For local development, the web project is configured with ASP.NET Core User Secrets. Placeholder values have been created in your local User Secrets `secrets.json` for:

```text
OpenAI:ApiKey
OpenAI:Model
```

Update them with real values:

```bash
dotnet user-secrets set --project src/BookNThings.Web "OpenAI:ApiKey" "sk-..."
```

Docker Compose reads:

```text
OPENAI_API_KEY
```

Copy `.env.example` to `.env` and fill in real values.

## Local Development

```bash
dotnet restore
dotnet run --project src/BookNThings.Web
```

Open the displayed local URL and use:

- `/` for the dashboard
- `/search` to find and save books
- `/books-read` to browse books read, sorted by newest read date first
- `/settings` to check configuration status

## Docker

```bash
docker compose up --build
```

Then open:

```text
http://localhost:8080
```

## Testing

```bash
dotnet test
```

Tests use xUnit, FluentAssertions, Moq, and Microsoft.NET.Test.Sdk.

## Screenshots

Add screenshots here after running the app locally:

- Dashboard
- Search results
- Books Read
- Settings

## Future Enhancements

- Ratings and reviews
- Tagging system
- Recommendations
- AI-generated summaries
- Semantic search
- Import/export
- Authentication
- Background jobs
- Caching
- Pagination
- OpenTelemetry tracing
- .NET Aspire integration
