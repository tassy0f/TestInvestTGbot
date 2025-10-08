using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddGoodNotionTaskCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;

    public AddGoodNotionTaskCommand(IUserStateService stateService, INotionService notionService)
    {
        _stateService = stateService;
        _notionService = notionService;
    }

    public override string Name => "/notionaddtaskgood";
    public override string Description => "Добавляем таску в ноушен";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingUserDesignByNotionModel };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var isSucsess = await _notionService.AddInternalNotionTask();
        if (isSucsess)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Задача сохраненно"
            );
        }
        else
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Возникли проблемы с сохранением"
            );
        }

        await _stateService.ClearUserStateAsync(message.Chat.Id);
    }
}
