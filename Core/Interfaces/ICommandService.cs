using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Interfaces
{
    public interface ICommandService
    {
        Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
        //Task ExecuteCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
        Task SetCommandsAsync(ITelegramBotClient botClient, CancellationToken cancellationToken);
    }
}
