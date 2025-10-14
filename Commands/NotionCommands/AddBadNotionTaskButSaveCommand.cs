using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Core.Services;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddBadNotionTaskButSaveCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;
    private readonly ICommandExecutor _commandExecutor;

    public AddBadNotionTaskButSaveCommand(
        IUserStateService stateService,
        INotionService notionService,
        ICommandExecutor commandExecutor)
    {
        _stateService = stateService;
        _notionService = notionService;
        _commandExecutor = commandExecutor;
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

        var saveTaskMessage = new Message
        {
            From = message.From,
            Chat = message.Chat,
            Text = "/notionaddtaskgood",
            Date = DateTime.UtcNow
        };

        var requestTaskMessage = new Message
        {
            From = message.From,
            Chat = message.Chat,
            Text = "/notionaddtaskbad",
            Date = DateTime.UtcNow
        };

        await _commandExecutor.ExecuteCommandAsync(botClient, saveTaskMessage, cancellationToken);
        await _commandExecutor.ExecuteCommandAsync(botClient, requestTaskMessage, cancellationToken);
    }
}