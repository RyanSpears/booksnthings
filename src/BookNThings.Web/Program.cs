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

builder.Services.AddScoped<BookSearchOrchestrator>();
builder.Services.AddScoped<ConnectionStatusService>();
builder.Services.AddHttpClient<IBookSearchService, OpenAiBookSearchService>();
builder.Services.AddScoped<IMongoBookRepository, MongoBookRepository>();
builder.Services.AddScoped<JsonBookStore>();
builder.Services.AddScoped<SynchronizingBookRepository>();
builder.Services.AddScoped<IBookRepository>(provider => provider.GetRequiredService<SynchronizingBookRepository>());
builder.Services.AddScoped<IBookDataSynchronizer>(provider => provider.GetRequiredService<SynchronizingBookRepository>());
builder.Services.AddHostedService<BookDataAlignmentHostedService>();

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
