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
    private readonly IRedisService _redisService;

    public AddGoodNotionTaskCommand(IUserStateService stateService, INotionService notionService, IRedisService redisService)
    {
        _stateService = stateService;
        _notionService = notionService;
        _redisService = redisService;
    }

    public override string Name => "/notionaddtaskgood";
    public override string Description => "Добавляем таску в ноушен";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingUserDesignByNotionModel };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        var model = await _redisService.GetNotionTaskAsync(chatId);

        if (model == null) 
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "В редиссе не нашлось сохраненного кеша задачи"
            );
            return;
        }

        var isSucsess = await _notionService.AddInternalNotionTask(model);
        if (isSucsess)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Задача сохранена"
            );
            await _redisService.ClearNotionTaskAsync(chatId);
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Возникли проблемы с сохранением"
            );
        }

        await _stateService.ClearUserStateAsync(chatId);
    }
}
