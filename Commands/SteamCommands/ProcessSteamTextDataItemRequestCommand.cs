using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Data.Entities;
using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class ProcessSteamTextDataItemRequestCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly ISteamService _steamService;
    private readonly AppDbContext _db;

    public ProcessSteamTextDataItemRequestCommand(IUserStateService stateService, ISteamService steamService, AppDbContext db)
    {
        _stateService = stateService;
        _steamService = steamService; // delete
        _db = db;
    }

    public override string Name => "/processsteamtextdataitemrequest";
    public override string Description => "Обработка текста в файл базы";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingForOneSteamFile };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;

        var user = await _db.Users
        .Include(u => u.SteamHistory)
        .FirstOrDefaultAsync(u => u.Username == message.Chat.Username);

        var newSteamItem = ParseFromMessage(message.Text); // Добавить проверку на правильность заполнения шаблона
        newSteamItem.UserId = user.Id;
        _db.SteamHistoryData.Add(newSteamItem); 
        await _db.SaveChangesAsync();
        await _stateService.ClearUserStateAsync(message.From.Id);

        await botClient.SendMessage(
            id,
            $"Добавлен элемент с именем {newSteamItem.Name}",
            parseMode: ParseMode.Html);
    }

    private SteamHistoryDataItem ParseFromMessage(string message)
    {
        var item = new SteamHistoryDataItem();
        var culture = CultureInfo.InvariantCulture;

        var nameMatch = Regex.Match(message, @"Название:\s*(.+)");
        var pricePerUnitMatch = Regex.Match(message, @"Стоимость за единицу:\s*([\d.,]+)");
        var countMatch = Regex.Match(message, @"Количество:\s*(\d+)");
        var totalPriceMatch = Regex.Match(message, @"Общая стоимость:\s*([\d.,]+)");

        if (nameMatch.Success)
            item.Name = nameMatch.Groups[1].Value.Trim();

        if (pricePerUnitMatch.Success &&
            decimal.TryParse(pricePerUnitMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, culture, out var pricePerUnit))
            item.PricePerUnit = pricePerUnit;

        if (countMatch.Success &&
            int.TryParse(countMatch.Groups[1].Value, out var count))
            item.Count = count;

        if (totalPriceMatch.Success &&
            decimal.TryParse(totalPriceMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, culture, out var total))
            item.PriceForAll = total;

        return item;
    }
}
