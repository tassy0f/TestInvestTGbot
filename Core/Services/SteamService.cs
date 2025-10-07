using MyTestTelegramBot.Core.Interfaces;
using System.Globalization;
using System.Text.Json;
using Telegram.Bot;

namespace MyTestTelegramBot.Core.Services
{
    public class SteamService : ISteamService
    {
        private readonly HttpClient _http;

        public SteamService()
        {
            _http = new HttpClient();
        }

        public async Task<decimal?> GetCurrentPriceAsync(string itemName, int appId = 730, int currency = 5)
        {
            var url = $"https://steamcommunity.com/market/priceoverview/?currency={currency}&appid={appId}&market_hash_name={Uri.EscapeDataString(itemName)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
                return null;

            if (doc.RootElement.TryGetProperty("lowest_price", out var priceEl))
            {
                var priceStr = priceEl.GetString();

                if (string.IsNullOrEmpty(priceStr)) return null;

                priceStr = priceStr.Replace("руб.", "").Replace(" ", "").Replace(",", ".");
                if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    return price;
            }

            return null;
        }
    }
}
