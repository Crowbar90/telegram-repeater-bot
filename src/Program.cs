using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TelegramRepeaterBot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        // Bind the "Bot" config section → BotSettings
        services
            .AddOptions<BotSettings>()
            .Bind(ctx.Configuration.GetSection(BotSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the Telegram bot client as a singleton
        services.AddSingleton<ITelegramBotClient>(_ =>
        {
            var token = ctx.Configuration["Bot:Token"]
                ?? throw new InvalidOperationException(
                    "Bot token is missing. Set BOT__TOKEN environment variable.");
            return new TelegramBotClient(token);
        });

        services.AddHostedService<RepeaterService>();
    })
    .Build();

await host.RunAsync();
