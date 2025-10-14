using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Services
{
    public class CommandService : ICommandService
    {
        private readonly ICommandExecutor _commandExecutor;

        public CommandService(
            ICommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        public Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            return _commandExecutor.ExecuteCommandAsync(botClient, message, cancellationToken);
        }

        //public async Task SetCommandsAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        //{
        //    var botCommands = _commands
        //        .Where(c => c.IsVisible)
        //        .Select(c => new BotCommand { Command = c.Name.Trim('/'), Description = c.Description })
        //        .ToArray();

        //    //await botClient.SetMyCommandsAsync(botCommands, cancellationToken: cancellationToken);
        //}
    }
}
