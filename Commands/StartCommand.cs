using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace MyTestTelegramBot.Commands;

public class StartCommand : BaseCommand
{
    public override string Name => "/start";
    public override string Description => "Запуск бота";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine("Command /start was called");
        //var currencyList = await _currencyService.GetValuteRateListAsync(new string[] { "USD", "EUR" });


        //var keyboard = new ReplyKeyboardMarkup(new[]
        //{
        //    new[] { new KeyboardButton($"📊 Курс доллара: {currencyList[0]?.Value} ₽") },
        //    new[] { new KeyboardButton($"📊 Курс евро: {currencyList[1]?.Value} ₽") },
        //    new[] { new KeyboardButton("🔒 Регистрация (недоступно)") }
        //})
        //{
        //    ResizeKeyboard = true
        //};
        var welcomeText = """
        <b><u>📊 Опции бота</u></b>

        <i>💵 Курсы валют:</i>
        /avgyearusdcourseforthisyear — USD за год

        <i>💵 Steam:</i>
        /steamMenu

        <i>💵 Notion:</i>
        /addnotiontask

        <i>⚙️ Акции:</i>
        /portfolio — Портфель
        /favorites — Избранные активы
        /searchbyticket — Поиск акции
        """;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Notion", "notion_command"),
                InlineKeyboardButton.WithCallbackData("🎮 Steam", "steam_command")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💵 Valute", "valute_command"),
                InlineKeyboardButton.WithCallbackData("📈 Invest", "invest_command")
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
