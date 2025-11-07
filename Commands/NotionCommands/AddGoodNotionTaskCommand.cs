using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Data.Entities;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class AddGoodNotionTaskCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;
    private readonly IRedisService _redisService;
    private readonly AppDbContext _db;

    public AddGoodNotionTaskCommand(IUserStateService stateService, INotionService notionService, IRedisService redisService, AppDbContext db)
    {
        _stateService = stateService;
        _notionService = notionService;
        _redisService = redisService;
        _db = db;
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
            var prefix = model.Title.Split('-')[0].Trim();
            var user = await _db.Users
                .Include(u => u.NotionCategories)
                .FirstOrDefaultAsync(u => u.Username == message.Chat.Username);

            var existing = await _db.NotionCategoryStats
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Prefix == prefix);

            if (existing == null)
            {
                existing = new NotionCategoryStats
                {
                    UserId = user.Id,
                    Prefix = prefix,
                    TaskCount = 1,
                    LastUpdated = DateTime.UtcNow
                };
                _db.NotionCategoryStats.Add(existing);
            }
            else
            {
                existing.TaskCount += 1;
                existing.LastUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
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
