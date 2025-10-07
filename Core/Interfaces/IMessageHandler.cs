using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Interfaces
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
    }
}
