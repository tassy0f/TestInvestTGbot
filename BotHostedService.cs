using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using MyTestTelegramBot.Controllers;

namespace MyTestTelegramBot;

public class BotHostedService : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotController _botController;
    private readonly CancellationTokenSource _cts;

    public BotHostedService(ITelegramBotClient botClient, BotController botController)
    {
        _botClient = botClient;
        _botController = botController;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _botClient.DeleteWebhook(cancellationToken: _cts.Token);

        _botClient.StartReceiving(
            updateHandler: _botController.HandleUpdateAsync,
            errorHandler: _botController.HandleErrorAsync,
            receiverOptions: new ReceiverOptions(),
            cancellationToken: _cts.Token
        );

        Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        Console.WriteLine("Бот остановлен.");
        return Task.CompletedTask;
    }
}