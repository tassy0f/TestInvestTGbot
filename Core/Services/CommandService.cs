using Microsoft.Extensions.Logging;
using MyTestTelegramBot.Commands;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Services
{
    public class CommandService : ICommandService
    {
        private readonly IEnumerable<BaseCommand> _commands;
        private readonly ILogger<CommandService> _logger;
        private readonly IUserStateService _userStateService;

        public CommandService(IEnumerable<BaseCommand> commands, ILogger<CommandService> logger, IUserStateService userStateService)
        {
            _commands = commands;
            _logger = logger;
            _userStateService = userStateService;
        }

        public async Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            foreach (var command in _commands)
            {
                if (await command.CanExecute(message, _userStateService))
                {
                    await command.ExecuteAsync(botClient, message, cancellationToken);
                    return;
                }
            }

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Команда не распознана",
                cancellationToken: cancellationToken);
        }

        public async Task SetCommandsAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var botCommands = _commands
                .Where(c => c.IsVisible)
                .Select(c => new BotCommand { Command = c.Name.Trim('/'), Description = c.Description })
                .ToArray();

            //await botClient.SetMyCommandsAsync(botCommands, cancellationToken: cancellationToken);
        }
    }
}
