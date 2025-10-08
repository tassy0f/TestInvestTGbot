using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MyTestTelegramBot.Controllers;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Core.Models.Settings;
using MyTestTelegramBot.Data.Repository;
using MyTestTelegramBot.Core.Services;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Handlers;
using Google.Api;
using MyTestTelegramBot.Commands;
using System.Reflection;

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

                var commandTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(BaseCommand)) && !t.IsAbstract);

                foreach (var type in commandTypes)
                {
                    services.AddScoped(typeof(BaseCommand), type);
                }

                // Регистрация сервисов
                services.AddScoped<ISteamService, SteamService>();
                services.AddScoped<INotionService, NotionService>();
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramSettings.BotToken));
                services.AddScoped<ITinkoffService, TinkoffService>();
                services.AddScoped<ICurrencyService, CurrencyService>();
                services.AddScoped<ICommandService, CommandService>();
                services.AddScoped<IMessageHandler, MessageHandler>();
                services.AddScoped<IUserStateService, UserStateService>();
                services.AddScoped<UpdateHandler>();

                services.AddHostedService<BotHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}