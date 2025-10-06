using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MyTestTelegramBot.Controllers;
using MyTestTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using MyTestTelegramBot.Models.Settings;
using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Models.DBContext;

namespace MyTestTelegramBot;

class Program
{
    static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.Development.json", // Для запуска локально создай такой файл, перенеси настройки из appsettings.json, задай там свои параметры
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
                services.Configure<PostgressSettings>(
                    builder.Configuration.GetSection("PostgressSettings"));
                services.Configure<NotionSettings>(
                    builder.Configuration.GetSection("NotionSettings"));


                var telegramSettings = builder.Configuration.GetSection("TelegramSettings").Get<TelegramSettings>();
                var tinkoffApiSettings = builder.Configuration.GetSection("TinkoffApiSettings").Get<TinkoffApiSettings>();
                var postgressSettings = builder.Configuration.GetSection("PostgressSettings").Get<PostgressSettings>();
                var notionSettings = builder.Configuration.GetSection("NotionSettings").Get<NotionSettings>();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql($"Host={postgressSettings.Host};Database={postgressSettings.Database};Username={postgressSettings.Username};Password={postgressSettings.Password}"));

                // Регистрация сервисов
                services.AddTransient<SteamService>();
                services.AddSingleton(sp => new NotionService(notionSettings.AuthToken));
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramSettings.BotToken));
                services.AddTransient<TinkoffService>();
                services.AddTransient<CurrencyService>();
                services.AddSingleton<CommandController>();
                services.AddSingleton<BotController>();

                services.AddHostedService<BotHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}