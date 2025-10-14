using MyTestTelegramBot.Core.Models;

namespace MyTestTelegramBot.Core.Interfaces;

public interface INotionService
{
    Task<bool> AddInternalNotionTask(NotionTaskModel model);
    Task<bool> AddTaskAsync(string databaseId, string title, DateTime date, string? note = null);

    NotionTaskModel ParseStringToNotionModel(string text);
}
