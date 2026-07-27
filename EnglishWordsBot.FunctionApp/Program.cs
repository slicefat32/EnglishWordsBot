using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using EnglishWordsBot.DAL;
using EnglishWordsBot.DAL.Services;
using EnglishWordsBot.FunctionApp;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register DbContext
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<EnglishWordsBotDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register services
builder.Services.AddScoped<CalendarWordsService>();

// Register Telegram Bot Client as singleton
builder.Services.AddSingleton(sp =>
{
    var botToken = builder.Configuration["TelegramBotToken"]
        ?? throw new InvalidOperationException("TelegramBotToken not found in configuration.");
    return new Telegram.Bot.TelegramBotClient(botToken);
});

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var host = builder.Build();

// Initialize database and cache on startup
using (var scope = host.Services.CreateScope())
{
    var calendarService = scope.ServiceProvider.GetRequiredService<CalendarWordsService>();
    await calendarService.InitializeDatabaseAsync();
    await WordsCache.LoadCacheAsync(host.Services);
    Console.WriteLine($"Cache loaded: {WordsCache.GetWordsCount()} words");
}

host.Run();
