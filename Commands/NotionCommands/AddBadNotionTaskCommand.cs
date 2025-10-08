using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Core.Services;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddBadNotionTaskCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;

    public AddBadNotionTaskCommand(IUserStateService stateService, INotionService notionService)
    {
        _stateService = stateService;
        _notionService = notionService;
    }

    public override string Name => "/notionaddtaskbad";
    public override string Description => "Не добавляем таску и просим заново отправить голосовое";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingUserDesignByNotionModel };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Ожидаю новое голосовое сообщение",
            cancellationToken: cancellationToken);

        await _stateService.SetUserStateAsync(message.Chat.Id, StateEnum.WaitingForNotionAudio);
    }
}