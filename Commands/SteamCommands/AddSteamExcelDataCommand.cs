using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class AddSteamExcelDataCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly ISteamService _steamService;

    public AddSteamExcelDataCommand(IUserStateService stateService, ISteamService steamService)
    {
        _stateService = stateService;
        _steamService = steamService;
    }

    public override string Name => "/addsteamexceldata";
    public override string Description => "Добавить выгрузку в базу";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;

        await botClient.SendMessage(
            chatId: id,
            text: "Окей ✅ Теперь пришли мне Excel (.xlsx) файл с историей покупок наклеек\n" +
            "Названия элементов должны быть строго на английском!"
        );

        await _stateService.SetUserStateAsync(id, StateEnum.WaitingForSteamExcelFile);
    }
}
