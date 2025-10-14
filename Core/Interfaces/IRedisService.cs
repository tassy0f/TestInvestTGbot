using MyTestTelegramBot.Core.Models;

namespace MyTestTelegramBot.Core.Interfaces;

public interface IRedisService
{
    public Task<NotionTaskModel?> GetNotionTaskAsync(long chatId);

    public Task SetNotionTaskAsync(long chatId, NotionTaskModel model);

    public Task ClearNotionTaskAsync(long chatId);
}