using MyTestTelegramBot.Core.Models;

namespace MyTestTelegramBot.Core.Interfaces;

public interface IUserStateService
{
    Task<UserState> GetUserStateAsync(long userId);
    Task SetUserStateAsync(long userId, string state);
    Task ClearUserStateAsync(long userId);
}
