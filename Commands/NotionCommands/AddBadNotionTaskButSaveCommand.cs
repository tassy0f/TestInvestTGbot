using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddBadNotionTaskButSaveCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;
    private readonly ICommandService _commandService;

    public AddBadNotionTaskButSaveCommand(IUserStateService stateService, INotionService notionService, ICommandService commandService)
    {
        _stateService = stateService;
        _notionService = notionService;
        _commandService = commandService;
    }

    public override string Name => "/notionaddtaskbadbutsave";
    public override string Description => "Добавляем таску и просим заново отправить голосовое";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingUserDesignByNotionModel };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Начинаем процесс сохранения записи",
            cancellationToken: cancellationToken);

        var anotherMessage = new Message
        {
            From = message.From,
            Chat = message.Chat,
            Text = "/notionaddtaskgood",
            Date = DateTime.UtcNow
        };

        await _commandService.ExecuteCommandAsync(botClient, anotherMessage, cancellationToken);

        await _stateService.SetUserStateAsync(message.Chat.Id, StateEnum.WaitingForNotionAudio);
    }
}