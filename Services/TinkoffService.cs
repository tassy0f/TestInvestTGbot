using Microsoft.Extensions.Options;
using MyTestTelegramBot.Models.Settings;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace MyTestTelegramBot.Services;

public class TinkoffService
{
    private readonly InvestApiClient _client;
    private readonly string _accountId;

    public TinkoffService(IOptions<TinkoffApiSettings> settings)
    {
        var token = settings.Value.ApiToken;
        _accountId = settings.Value.AccountId;
        _client = InvestApiClientFactory.Create(token);
    }

    public InvestApiClient GetClient() => _client;

    public async Task<string> GetPortfolioInfoAsync()
    {
        try
        {
            var portfolio = await _client.Operations.GetPortfolioAsync(
                new PortfolioRequest { AccountId = _accountId });

            if (portfolio.Positions.Count == 0)
                return "Портфель пуст";

            var result = new StringBuilder();
            foreach (var position in portfolio.Positions)
            {
                result.AppendLine($"▪️ {position.Figi}: {position.Quantity} шт.");
            }
            return result.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка получения портфеля: {ex.Message}");
        }
    }

    public async Task<(string, MessageEntity[])> GetFavoriteInstrumentsInfoAsync()
    {
        try
        {
            Console.WriteLine("Запрос избранных активов...");
            var entities = new List<MessageEntity>();
            var response = await _client.Instruments.GetFavoritesAsync(new GetFavoritesRequest());
            if (!response.FavoriteInstruments.Any())
                return ("Избранные активы отсутствуют.", entities.ToArray());

            var result = new System.Text.StringBuilder();
           
            result.AppendLine("📊 <b>Избранные активы:</b>");

            foreach (var instrument in response.FavoriteInstruments)
            {
                decimal price;
                string currency;
                try
                {
                    price = await GetStockPriceAsync(instrument.Figi);
                    currency = "RUB";
                }
                catch (Exception ex)
                {
                    price = 0;
                    currency = "N/A";
                    Console.WriteLine($"Ошибка получения цены для {instrument.Ticker}: {ex.Message}");
                }

                var (emoji, customEmojiId) = GetCompanyEmojiInfo(instrument.Ticker);
                result.AppendLine($"▪️   {emoji} {instrument.Name} ({instrument.Ticker}, {instrument.Figi})");
                result.AppendLine($"💵 Цена: {(price > 0 ? $"{price:N2} {currency}" : "Н/Д")}");
                result.AppendLine($"---------------------------------------------------------");
                if (!string.IsNullOrEmpty(customEmojiId))
                {
                    entities.Add(new MessageEntity
                    {
                        Type = MessageEntityType.CustomEmoji,
                        Offset = 2,
                        Length = emoji.Length,
                        CustomEmojiId = customEmojiId
                    });
                }
            }
            return (result.ToString(), entities.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в GetFavoriteInstrumentsInfoAsync: {ex.Message}");
            throw new Exception($"Ошибка получения избранных активов: {ex.Message}");
        }
    }

    private (string Emoji, string CustomEmojiId) GetCompanyEmojiInfo(string ticker)
    {
        var emojiMap = new Dictionary<string, (string Emoji, string CustomEmojiId)>
    {
        { "SBER", ("🏦", "5433863928499150581") },
        { "GAZP", ("🔥", "5318768971153941318") },
        { "LKOH", ("🛢️", "5377348266327285808") },
        { "YNDX", ("🔍", "5204140588990998268") }
    };
        return emojiMap.TryGetValue(ticker.ToUpper(), out var value)
            ? value
            : ("📈", "");
    }

    public async Task<Share?> FindStockAsync(string ticker)
    {
        try
        {
            var response = await _client.Instruments.SharesAsync(new InstrumentsRequest());
            return response.Instruments.FirstOrDefault(i =>
                i.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка поиска акции: {ex.Message}");
            return null;
        }
    }

    public async Task<decimal> GetStockPriceAsync(string figi)
    {
        try
        {
            var response = await _client.MarketData.GetLastPricesAsync(
                new GetLastPricesRequest { Figi = { figi } });

            if (response.LastPrices.Count == 0)
                throw new Exception("Цены не найдены");

            var price = response.LastPrices[0].Price;
            return (decimal)price.Units + (decimal)price.Nano / 1_000_000_000;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения цены: {ex.Message}");
            throw;
        }
    }
}