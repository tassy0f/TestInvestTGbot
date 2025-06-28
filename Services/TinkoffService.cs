using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace MyTestTelegramBot.Services;

public class TinkoffService
{
    private readonly InvestApiClient _client;

    public TinkoffService(string token)
    {
        _client = InvestApiClientFactory.Create(token);
    }

    public InvestApiClient GetClient() => _client;

    public async Task<string> GetPortfolioInfoAsync(string accountId)
    {
        try
        {
            var portfolio = await _client.Operations.GetPortfolioAsync(
                new PortfolioRequest { AccountId = accountId });

            if (portfolio.Positions.Count == 0)
                return "Портфель пуст";

            var result = new System.Text.StringBuilder();
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
}