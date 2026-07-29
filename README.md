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
- IGDB client ID and client secret

The app stores books in `books.json`, games in `games.json`, and TV shows in `show.json` inside the configured local JSON storage folder.

## OpenAI Setup

Create an OpenAI API key and configure:

```powershell
$env:OpenAI__ApiKey="sk-..."
$env:OpenAI__Model="gpt-4.1-mini"
```

The OpenAI integration uses the Responses API with strict JSON schema structured outputs.

## IGDB Setup

Games now search IGDB first and fall back to OpenAI when IGDB is unavailable, misconfigured, or returns no grounded match.

Create a Twitch developer application for IGDB, then configure:

```powershell
$env:IGDB__ClientId="..."
$env:IGDB__ClientSecret="..."
```

IGDB uses the Twitch client credentials flow to exchange those values for a short-lived access token before calling the games API.

## TMDb Setup

Movies now search TMDb first and only fall back to OpenAI when TMDb cannot find a grounded match.

Create a TMDb v4 read access token and configure:

```powershell
$env:TMDb__BearerToken="eyJ..."
```

TMDb is used as the source of truth for movie metadata, then the app maps the response into the local movie JSON shape.

## Environment Variables

Local development:

```text
OpenAI__ApiKey
OpenAI__Model
IGDB__ClientId
IGDB__ClientSecret
TMDb__BearerToken
```

## User Secrets

For local development, the web project is configured with ASP.NET Core User Secrets. Placeholder values have been created in your local User Secrets `secrets.json` for:

```text
OpenAI:ApiKey
OpenAI:Model
IGDB:ClientId
IGDB:ClientSecret
```

Update them with real values:

```bash
dotnet user-secrets set --project src/BookNThings.Web "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set --project src/BookNThings.Web "IGDB:ClientId" "..."
dotnet user-secrets set --project src/BookNThings.Web "IGDB:ClientSecret" "..."
```

Docker Compose reads:

```text
OPENAI_API_KEY
OPENAI_MODEL
IGDB_CLIENT_ID
IGDB_CLIENT_SECRET
ASPNETCORE_ENVIRONMENT
BOOKNTHINGS_PORT
BOOKNTHINGS_HOST_DATA_DIRECTORY
BOOKNTHINGS_CONTAINER_DATA_DIRECTORY
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

The container stores JSON data in a host-mounted folder instead of inside the image.
By default the compose file maps `/Users/ryanspears/OneDrive/Dev/Projects/BooksNThings/Data`
on your machine to `/data` in the container.
The app is configured to use the container path from `BOOKNTHINGS_CONTAINER_DATA_DIRECTORY`.

Update `.env` if you want to change:

- the host folder that gets mounted
- the container folder the app sees
- the published port
- the ASP.NET Core environment

```bash
docker compose up --build
```

Then open:

```text
http://localhost:8080
```

If you want a different storage location, change the bind mount in `docker-compose.yml`
or, preferably, update `BOOKNTHINGS_HOST_DATA_DIRECTORY` and
`BOOKNTHINGS_CONTAINER_DATA_DIRECTORY` in `.env`.

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
