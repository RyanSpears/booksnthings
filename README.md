# BookNThings

BookNThings is a local-deployable .NET 10 Blazor Web App for finding books with natural language, validating OpenAI structured JSON outputs, saving selected books to MongoDB Atlas with a read date, and browsing books read.

## Architecture

- `src/BookNThings.Web`: Blazor Web App UI, routing, forms, configuration bootstrap, dependency injection, and user-friendly error handling.
- `src/BookNThings.Application`: service contracts, domain model, validation, and orchestration logic without external integration details.
- `src/BookNThings.Infrastructure`: MongoDB Atlas repository, OpenAI structured output integration, configuration models, and connection checks.
- `tests/BookNThings.Tests`: xUnit tests covering models, validation, application services, repository contracts, and OpenAI parsing.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- MongoDB Atlas M0 cluster
- OpenAI API key

## MongoDB Atlas Setup

1. Create a free M0 cluster in MongoDB Atlas.
2. Create a database user with read/write permissions.
3. Add your local IP address to Network Access.
4. Copy the `mongodb+srv://...` connection string.
5. Set `MongoDb__ConnectionString` locally, or use `MONGODB_CONNECTION_STRING` with Docker Compose.

The app stores books in database `booknthings`, collection `books`, unless overridden.

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
MongoDb__ConnectionString
MongoDb__DatabaseName
MongoDb__BooksCollection
```

## User Secrets

For local development, the web project is configured with ASP.NET Core User Secrets. Placeholder values have been created in your local User Secrets `secrets.json` for:

```text
OpenAI:ApiKey
OpenAI:Model
MongoDb:ConnectionString
MongoDb:DatabaseName
MongoDb:BooksCollection
```

Update them with real values:

```bash
dotnet user-secrets set --project src/BookNThings.Web "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set --project src/BookNThings.Web "MongoDb:ConnectionString" "mongodb+srv://..."
```

Docker Compose reads:

```text
MONGODB_CONNECTION_STRING
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
- Local MongoDB container option
- Authentication
- Background jobs
- Caching
- Pagination
- OpenTelemetry tracing
- .NET Aspire integration
