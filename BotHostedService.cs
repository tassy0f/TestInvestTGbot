using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using MyTestTelegramBot.Controllers;
using MyTestTelegramBot.Handlers;

namespace MyTestTelegramBot;

public class BotHostedService : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly UpdateHandler _updateHandler;
    private readonly CancellationTokenSource _cts;

    public BotHostedService(ITelegramBotClient botClient, UpdateHandler updateHandler)
    {
        _botClient = botClient;
        _cts = new CancellationTokenSource();
        _updateHandler = updateHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _botClient.DeleteWebhook(cancellationToken: _cts.Token);

        _botClient.StartReceiving(
            updateHandler: _updateHandler.HandleUpdateAsync,
            errorHandler: _updateHandler.HandleErrorAsync,
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