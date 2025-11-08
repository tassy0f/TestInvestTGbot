using MyTestTelegramBot.Core.Models;

namespace MyTestTelegramBot.Core.Interfaces;

public interface ICurrencyService
{
    Task<Currency?> GetValuteRateAsync(string valuteCode);

    Task<List<Currency>?> GetValuteRateListAsync(string[] valuteArr);

    Task<string> GetValuteRateFormatAsync(string valuteCode);

    Task<decimal> GetAverageUsdRateForYearAsync(int year);
}
