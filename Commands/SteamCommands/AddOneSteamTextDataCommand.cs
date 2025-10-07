using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class AddOneSteamTextDataCommand : BaseCommand
{
    private readonly IUserStateService _stateService;

    public AddOneSteamTextDataCommand(IUserStateService stateService)
    {
        _stateService = stateService;
    }

    public override string Name => "/addonesteamtextdata";
    public override string Description => "Добавить один элемент в базу";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;
        await botClient.SendMessage(
                chatId: id,
                text: "Скопируйте сообщение ниже и отправтье боту заполненый шаблон (Название строго на английском):"
        );

        await botClient.SendMessage(
            chatId: id,
            text: "Название:               \n" +
                  "Стоимость за единицу:   \n" +
                  "Количество:             \n" +
                  "Общая стоимость:        \n"
        );

        await _stateService.SetUserStateAsync(id, StateEnum.WaitingForOneSteamFile);
    }
}
