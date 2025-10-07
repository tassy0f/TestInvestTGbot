using System.Net.Http.Json;
using HtmlAgilityPack;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Models;
using MyTestTelegramBot.Core.Models.Errors;

namespace MyTestTelegramBot.Core.Services;

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;

    public CurrencyService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<Currency?> GetValuteRateAsync(string valuteCode)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CbrResponse>(
                "https://www.cbr-xml-daily.ru/daily_json.js");
            if (response?.Valute.TryGetValue(valuteCode, out var currency) == true)
            {
                return currency;
            }

            throw new ValuteException()
            {
                Message = $"Проблема с доступом к валюте: {valuteCode}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new Currency()
            {
                CharCode = valuteCode,
                Value = 0
            };
        }
    }

    public async Task<List<Currency>?> GetValuteRateListAsync(string[] valuteArr)
    {
        var currencyList = new List<Currency>();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CbrResponse>(
                "https://www.cbr-xml-daily.ru/daily_json.js");
            foreach (var valute in valuteArr)
            {
                if (response?.Valute.TryGetValue(valute, out var currency) == true)
                {
                    currencyList.Add(currency);
                }
            }
            return currencyList;

            throw new ValuteException()
            {
                Message = $"Проблема с доступом к валютам"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return currencyList;
        }
    }

    public async Task<string> GetValuteRateFormatAsync(string valuteCode)
    {
        try
        {
            var currency = await GetValuteRateAsync(valuteCode);
            return $"<b>🏦 Курс {currency?.Name}</b>\n" +
                    $"➡️ <b>{currency?.Nominal} {valuteCode} = {currency?.Value} RUB</b>\n" +
                    $"📅 Обновлено: {DateTime.Now:dd.MM.yyyy HH:mm}";
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