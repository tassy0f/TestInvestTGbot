using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Text;
using MyTestTelegramBot.Data.Repository;
using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Data.Entities;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class CreateSteamPriceTableCommand : BaseCommand
{
    
    private readonly AppDbContext _db;
    private readonly ISteamService _steamService;

    public CreateSteamPriceTableCommand(AppDbContext db, ISteamService steamService)
    {
        _db = db;
        _steamService = steamService;
    }

    public override string Name => "/createsteampricetable";
    public override string Description => "Сгенерировать таблицу прибыли";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;
        var user = await _db.Users
       .Include(u => u.SteamHistory)
       .FirstOrDefaultAsync(u => u.Username == message.Chat.Username);

        if (user == null || !user.SteamHistory.Any())
        {
            await botClient.SendMessage(id, "Нет данных по твоим покупкам. Сначала загрузи Excel через /addsteamexceldata");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("📊 Сравнение цен:");
        var unitedResult = UniteSteamDataItems(user.SteamHistory);

        foreach (var item in unitedResult)
        {
            var currentPrice = await _steamService.GetCurrentPriceAsync(item.Name);
            if (currentPrice != null)
            {
                var diff = currentPrice.Value - item.PricePerUnit;
                string diffEmoji = diff < 0 ? "🔻" : diff > 0 ? "🟩" : "⚪";
                sb.AppendLine($"{item.Name}\n Куплено за: {item.PricePerUnit}₽ | Сейчас: {currentPrice}₽ | {diffEmoji} {diff:+0.00;-0.00}₽\n");
            }
            else
            {
                sb.AppendLine($"{item.Name} → ❌ Не удалось получить цену");
            }
        }

        await botClient.SendMessage(id, sb.ToString());
    }

    private List<SteamHistoryDataItem> UniteSteamDataItems(ICollection<SteamHistoryDataItem> steamHistory)
    {
        return steamHistory
            .GroupBy(s => s.Name)
            .Select(g => new SteamHistoryDataItem
            {
                Name = g.Key,
                Count = g.Sum(x => x.Count),
                PriceForAll = g.Sum(x => x.PriceForAll),
                PricePerUnit = g.Sum(x => x.PriceForAll) / g.Sum(x => x.Count)
            })
            .ToList();
    }
}
