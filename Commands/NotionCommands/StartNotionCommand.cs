using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class StartNotionCommand : BaseCommand
{
    public override string Name => "/notionmenu";
    public override string Description => "Меню возможностей Notion";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Command /notionmenu was called by user {message.From?.Username}");

        var welcomeText = """
            <b>Управлять Notion</b>
            <i> Тут вы можете записать голосом свои задачи в ежедневник или в заметки, а также получить выгрузку своих задач </i>

            <i>Добавить тут описание....:</i> 
           """; // TODO

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить заметку", "notion_add_mark_command"),
                InlineKeyboardButton.WithCallbackData("Добавить задачу в Ежедневник", "notion_add_task_command")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Выгрузить заметки", "notion_get_marks_command"),
                InlineKeyboardButton.WithCallbackData("Выгрузить задачи на неделю", "notion_get_week_tasks_command"),
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
