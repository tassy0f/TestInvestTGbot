using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MyTestTelegramBot.Controllers;
using MyTestTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using MyTestTelegramBot.Models.Settings;

namespace MyTestTelegramBot;

class Program
{
    static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.Development.json",
                    optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((builder, services) =>
            {
                // Конфигурация настроек
                services.Configure<TelegramSettings>(
                    builder.Configuration.GetSection("TelegramSettings"));
                services.Configure<TinkoffApiSettings>(
                    builder.Configuration.GetSection("TinkoffApiSettings"));
                var telegramSettings = builder.Configuration.GetSection("TelegramSettings").Get<TelegramSettings>();
                var tinkoffApiSettings = builder.Configuration.GetSection("TinkoffApiSettings").Get<TinkoffApiSettings>();

                // Регистрация сервисов
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramSettings.BotToken));
                services.AddSingleton<TinkoffService>();
                services.AddSingleton<CurrencyService>();
                services.AddSingleton<CommandController>();
                services.AddSingleton<BotController>();

                // Регистрация хостед-сервиса для запуска бота
                services.AddHostedService<BotHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}