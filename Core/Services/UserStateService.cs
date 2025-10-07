using Microsoft.Extensions.Logging;
using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Models;

namespace MyTestTelegramBot.Core.Services;

public class UserStateService : IUserStateService
{
    private readonly Dictionary<long, UserState> _userStates = new();
    private readonly ILogger<UserStateService> _logger;

    public UserStateService(ILogger<UserStateService> logger)
    {
        _logger = logger;
    }

    public Task<UserState> GetUserStateAsync(long userId)
    {
        if (!_userStates.ContainsKey(userId))
        {
            _userStates[userId] = new UserState { UserId = userId };
        }

        return Task.FromResult(_userStates[userId]);
    }

    public Task SetUserStateAsync(long userId, string state)
    {
        if (!_userStates.ContainsKey(userId))
        {
            _userStates[userId] = new UserState { UserId = userId };
        }

        _userStates[userId].CurrentState = state;
        _userStates[userId].UpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    public Task ClearUserStateAsync(long userId)
    {
        if (_userStates.ContainsKey(userId))
        {
            _userStates[userId].CurrentState = StateEnum.MainMenu;
            _userStates[userId].Data.Clear();
        }

        return Task.CompletedTask;
    }
}
