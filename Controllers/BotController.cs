using MyTestTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyTestTelegramBot.Controllers;

public class BotController
{
    private readonly ITelegramBotClient _botClient;
    private readonly CommandController _commandController;

    public BotController(
        ITelegramBotClient botClient,
        TinkoffService tinkoffService,
        CurrencyService currencyService,
        StockService stockService)
    {
        _botClient = botClient;
        _commandController = new CommandController(botClient, tinkoffService, currencyService, stockService);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message is { } message)
        {
            if (message.Text is { } text)
            {
                if (text.StartsWith('/'))
                {
                    await _commandController.HandleCommandAsync(message);
                }
                else
                {
                    await _commandController.HandleTextMessageAsync(message);
                }
            }
        }
        else if (update.PollAnswer is { } pollAnswer)
        {
            await _commandController.HandlePollAnswerAsync(pollAnswer);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}