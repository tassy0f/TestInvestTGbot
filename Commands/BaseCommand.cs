using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Core.Interfaces;

namespace MyTestTelegramBot.Commands;

public abstract class BaseCommand
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual bool IsVisible => true;

    public virtual string[] RequiredStates => Array.Empty<string>();
    public virtual bool CheckState => false;

    public abstract Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    public virtual async Task<bool> CanExecute(Message message, IUserStateService stateService)
    {
        if (!CheckState)
            return message.Text?.StartsWith(Name) == true;

        var userState = await stateService.GetUserStateAsync(message.From.Id);
        return RequiredStates.Contains(userState.CurrentState);
    }
}
