using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace MyTestTelegramBot.Core.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string GetKey(long chatId) => $"user_session:{chatId}";

    public async Task<NotionTaskModel?> GetNotionTaskAsync(long chatId)
    {
        var value = await _db.StringGetAsync(GetKey(chatId));
        if (value.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<NotionTaskModel>(value!, _jsonOptions);
    }

    public async Task SetNotionTaskAsync(long chatId, NotionTaskModel model)
    {
        var json = JsonSerializer.Serialize(model, _jsonOptions);
        await _db.StringSetAsync(GetKey(chatId), json, TimeSpan.FromMinutes(30));
    }

    public async Task ClearNotionTaskAsync(long chatId)
    {
        await _db.KeyDeleteAsync(GetKey(chatId));
    }
}