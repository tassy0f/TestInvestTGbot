using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace MyTestTelegramBot.Services;

public class StockService
{
    private readonly InvestApiClient _client;

    public StockService(TinkoffService tinkoffService)
    {
        _client = tinkoffService.GetClient();
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