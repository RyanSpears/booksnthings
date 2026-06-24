using BookNThings.Web.Components;
using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Health;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.Mongo;
using BookNThings.Infrastructure.OpenAi;
using BookNThings.Web.Services;
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
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<LocalBooksOptions>(options =>
{
    options.DataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
    options.FileName = "books.json";
});
builder.Services.Configure<LocalGamesOptions>(options =>
{
    options.DataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
    options.FileName = "games.json";
});
builder.Services.Configure<LocalShowsOptions>(options =>
{
    options.DataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
    options.FileName = "show.json";
});

builder.Services.AddScoped<BookSearchOrchestrator>();
builder.Services.AddScoped<ShowSearchOrchestrator>();
builder.Services.AddScoped<GameSearchOrchestrator>();
builder.Services.AddScoped<ConnectionStatusService>();
builder.Services.AddHttpClient<IBookSearchService, OpenAiBookSearchService>();
builder.Services.AddHttpClient<IShowSearchService, OpenAiShowSearchService>();
builder.Services.AddHttpClient<IGameSearchService, OpenAiGameSearchService>();
builder.Services.AddScoped<IMongoBookRepository, MongoBookRepository>();
builder.Services.AddScoped<IMongoGameRepository, MongoGameRepository>();
builder.Services.AddScoped<IMongoShowRepository, MongoShowRepository>();
builder.Services.AddScoped<JsonBookStore>();
builder.Services.AddScoped<JsonGameStore>();
builder.Services.AddScoped<JsonShowStore>();
builder.Services.AddScoped<SynchronizingBookRepository>();
builder.Services.AddScoped<SynchronizingGameRepository>();
builder.Services.AddScoped<SynchronizingShowRepository>();
builder.Services.AddScoped<IBookRepository>(provider => provider.GetRequiredService<SynchronizingBookRepository>());
builder.Services.AddScoped<IBookDataSynchronizer>(provider => provider.GetRequiredService<SynchronizingBookRepository>());
builder.Services.AddScoped<IGameRepository>(provider => provider.GetRequiredService<SynchronizingGameRepository>());
builder.Services.AddScoped<IGameDataSynchronizer>(provider => provider.GetRequiredService<SynchronizingGameRepository>());
builder.Services.AddScoped<IShowRepository>(provider => provider.GetRequiredService<SynchronizingShowRepository>());
builder.Services.AddScoped<IShowDataSynchronizer>(provider => provider.GetRequiredService<SynchronizingShowRepository>());
builder.Services.AddHostedService<BookDataAlignmentHostedService>();
builder.Services.AddHostedService<GameDataAlignmentHostedService>();
builder.Services.AddHostedService<ShowDataAlignmentHostedService>();

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
