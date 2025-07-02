using Microsoft.Extensions.Options;
using MyTestTelegramBot.Models.Settings;
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