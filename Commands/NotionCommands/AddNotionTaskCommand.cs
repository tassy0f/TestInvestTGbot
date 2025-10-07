using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddNotionTaskCommand : BaseCommand
{
    private readonly IUserStateService _stateService;

    public AddNotionTaskCommand(IUserStateService stateService)
    {
        _stateService = stateService;
    }

    public override string Name => "/notionaddtask";
    public override string Description => "Добавить задачу в Notion";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;
        await botClient.SendMessage(
                chatId: id,
                text: "Произнесите вслух описание задачи. В голосовом сообщении должно быть четко слышны слова: Заголовок, Дата, Описание"
        );

        await _stateService.SetUserStateAsync(id, StateEnum.WaitingForNotionAudio);
    }
}
