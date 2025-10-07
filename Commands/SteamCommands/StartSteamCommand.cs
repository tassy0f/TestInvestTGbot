using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class StartSteamCommand : BaseCommand
{
    public override string Name => "/steammenu";
    public override string Description => "Меню возможностей Стима";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Command /steammenu was called by user {message.From?.Username}");

        var welcomeText = """
            <b>Инвестиции Steam</b>
            <i> Тут вы можете загрузить историю своих инвестиций в предметы, на платформе Steam </i>

            <i>Загрузить данные по покупке инвентаря:</i>
            /uploadSteamDataHistory
           
            <i>Сформировать таблицу стоимости:</i>
            /createsteampricetable
           
            <i>Загрузить одну единицу инвентаря:</i>
            /addsteamitem
           """;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить один предмет", "steam_addOneItem_command"),
                InlineKeyboardButton.WithCallbackData("Добавить выгрузку из Excel", "steam_addManyItems_command")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Расчитать таблицу прибыли на основе ваших данных", "steam_countData_command"),
            }
        });

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeText,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
