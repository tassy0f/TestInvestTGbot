using System.Net.Http.Json;
using HtmlAgilityPack;
using MyTestTelegramBot.Models;

namespace MyTestTelegramBot.Services;

public class CurrencyService
{
    private readonly HttpClient _httpClient;

    public CurrencyService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetValuteRateAsync(string valuteCode)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CbrResponse>(
                "https://www.cbr-xml-daily.ru/daily_json.js");

            if (response?.Valute.TryGetValue(valuteCode, out var currency) == true)
            {
                return $"<b>🏦 Курс {currency.Name}</b>\n" +
                       $"➡️ <b>{currency.Nominal} {valuteCode} = {currency.Value} RUB</b>\n" +
                       $"📅 Обновлено: {DateTime.Now:dd.MM.yyyy HH:mm}";
            }
            return "❌ Данные о валюте не найдены";
        }
        catch (Exception ex)
        {
            return $"⚠️ Ошибка: {ex.Message}";
        }
    }

    public async Task<decimal> GetAverageUsdRateForYearAsync(int year)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(
                $"https://www.cbr.ru/currency_base/dynamics/?UniDbQuery.Posted=True&UniDbQuery.so=1&UniDbQuery.mode=1&UniDbQuery.date_req1=&UniDbQuery.date_req2=&UniDbQuery.VAL_NM_RQ=R01235&UniDbQuery.From=01.01.{year}&UniDbQuery.To=31.12.{year}");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rates = doc.DocumentNode
                .SelectNodes("//table[@class='data']//tr[position()>1]/td[last()]")
                .Select(x => decimal.Parse(x.InnerText.Trim()))
                .ToList();

            return rates.Any() ? rates.Average() : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return 0;
        }
    }
}