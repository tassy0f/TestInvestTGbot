using Microsoft.Extensions.DependencyInjection;
using MyTestTelegramBot.Commands;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Services
{
    // Services/ICommandExecutor.cs
    public interface ICommandExecutor
    {
        Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
    }

    public class CommandExecutor : ICommandExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserStateService _userStateService;

        public CommandExecutor(IServiceProvider serviceProvider, IUserStateService userStateService)
        {
            _serviceProvider = serviceProvider;
            _userStateService = userStateService;
        }

        public async Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var commands = scope.ServiceProvider.GetServices<BaseCommand>();

            foreach (var command in commands)
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
    }
}
