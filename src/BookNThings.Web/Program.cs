using BookNThings.Web.Components;
using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Health;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.OpenAi;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;

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
builder.Services.Configure<LocalBooksOptions>(options => options.FileName = "books.json");
builder.Services.Configure<LocalGamesOptions>(options => options.FileName = "games.json");
builder.Services.Configure<LocalShowsOptions>(options => options.FileName = "show.json");
builder.Services.AddSingleton<LocalJsonStorageSettingsService>();
builder.Services.AddSingleton<ILocalJsonStorageSettings>(provider => provider.GetRequiredService<LocalJsonStorageSettingsService>());

builder.Services.AddScoped<BookSearchOrchestrator>();
builder.Services.AddScoped<ShowSearchOrchestrator>();
builder.Services.AddScoped<GameSearchOrchestrator>();
builder.Services.AddScoped<ConnectionStatusService>();
builder.Services.AddHttpClient<IBookSearchService, OpenAiBookSearchService>();
builder.Services.AddHttpClient<IShowSearchService, OpenAiShowSearchService>();
builder.Services.AddHttpClient<IGameSearchService, OpenAiGameSearchService>();
builder.Services.AddScoped<JsonBookStore>();
builder.Services.AddScoped<JsonGameStore>();
builder.Services.AddScoped<JsonShowStore>();
builder.Services.AddScoped<IBookRepository, JsonBookRepository>();
builder.Services.AddScoped<IGameRepository, JsonGameRepository>();
builder.Services.AddScoped<IShowRepository, JsonShowRepository>();

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
