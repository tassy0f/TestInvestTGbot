using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Services;
using MyTestTelegramBot.Data.Repository;
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
        ITinkoffService tinkoffService,
        ICurrencyService currencyService,
        ISteamService steamService,
        NotionService notionService,
        AppDbContext db)
    {
        _botClient = botClient;
        _commandController = new CommandController(botClient, tinkoffService, currencyService, steamService, notionService, db);
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
            else if (message.Document is { } document)
            {
                Console.WriteLine(document.FileName);
                if (document.FileName.EndsWith(".xlsx"))
                {
                    //await _commandController.HandleXlsxDocumentAsync(message);
                }
                else
                {
                    await _botClient.SendMessage(
                        message.Chat.Id,
                        text: "Пожалуйста, загрузите Excel (.xlsx) файл."
                        );
                }
            }
        }
        else if (update.PollAnswer is { } pollAnswer)
        {
            await _commandController.HandlePollAnswerAsync(pollAnswer);
        }
    }
}