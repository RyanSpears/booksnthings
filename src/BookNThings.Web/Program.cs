using BookNThings.Web.Components;
using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Health;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.OpenAi;
using BookNThings.Infrastructure.TvMaze;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys"));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keyDirectory)
    .SetApplicationName("BookNThings");

builder.Services.AddMudServices();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<LocalJsonStorageOptions>(builder.Configuration.GetSection(LocalJsonStorageOptions.SectionName));
builder.Services.Configure<LocalBooksOptions>(options => options.FileName = "books.json");
builder.Services.Configure<LocalGamesOptions>(options => options.FileName = "games.json");
builder.Services.Configure<LocalShowsOptions>(options => options.FileName = "show.json");
builder.Services.Configure<LocalMoviesOptions>(options => options.FileName = "movies.json");
builder.Services.AddSingleton<LocalJsonStorageSettingsService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<LocalJsonStorageSettingsService>>();
    var storageOptions = provider.GetRequiredService<IOptions<LocalJsonStorageOptions>>().Value;
    var defaultDataDirectory = string.IsNullOrWhiteSpace(storageOptions.DefaultDataDirectory)
        ? Path.Combine(builder.Environment.ContentRootPath, "Data")
        : storageOptions.DefaultDataDirectory;

    return new LocalJsonStorageSettingsService(logger, defaultDataDirectory: defaultDataDirectory);
});
builder.Services.AddSingleton<ILocalJsonStorageSettings>(provider => provider.GetRequiredService<LocalJsonStorageSettingsService>());

builder.Services.AddScoped<BookSearchOrchestrator>();
builder.Services.AddScoped<ShowSearchOrchestrator>();
builder.Services.AddScoped<GameSearchOrchestrator>();
builder.Services.AddScoped<MovieSearchOrchestrator>();
builder.Services.AddScoped<ConnectionStatusService>();
builder.Services.AddHttpClient<IBookSearchService, OpenAiBookSearchService>();
builder.Services.AddHttpClient<OpenAiShowSearchService>();
builder.Services.AddHttpClient<IShowSearchService, TvMazeShowSearchService>(client =>
{
    client.BaseAddress = new Uri("https://api.tvmaze.com/");
});
builder.Services.AddHttpClient<IGameSearchService, OpenAiGameSearchService>();
builder.Services.AddHttpClient<IMovieSearchService, OpenAiMovieSearchService>();
builder.Services.AddScoped<JsonBookStore>();
builder.Services.AddScoped<JsonGameStore>();
builder.Services.AddScoped<JsonShowStore>();
builder.Services.AddScoped<JsonMovieStore>();
builder.Services.AddScoped<IBookRepository, JsonBookRepository>();
builder.Services.AddScoped<IGameRepository, JsonGameRepository>();
builder.Services.AddScoped<IShowRepository, JsonShowRepository>();
builder.Services.AddScoped<IMovieRepository, JsonMovieRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
.AddInteractiveServerRenderMode();

app.Run();
