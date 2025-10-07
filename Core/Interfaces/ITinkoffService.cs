
using Telegram.Bot.Types;
using Tinkoff.InvestApi.V1;

namespace MyTestTelegramBot.Core.Interfaces
{
    public interface ITinkoffService
    {
        Task<string> GetPortfolioInfoAsync();

        Task<(string, MessageEntity[])> GetFavoriteInstrumentsInfoAsync();

        Task<Share?> FindStockAsync(string ticker);

        Task<decimal> GetStockPriceAsync(string figi);
    }
}