using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Services;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTestTelegramBot.Controllers;

public class CommandController
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITinkoffService _tinkoffService;
    private readonly ICurrencyService _currencyService;
    private readonly ISteamService _steam;
    private readonly NotionService _notion;
    private readonly AppDbContext _db;
    private readonly Dictionary<long, string> _awaitingTicker = new();
    private static readonly Dictionary<long, string> _userStates = new();

    public CommandController(
        ITelegramBotClient botClient,
        ITinkoffService tinkoffService,
        ICurrencyService currencyService,
        ISteamService steam,
        NotionService notion,
        AppDbContext db)
    {
        _botClient = botClient;
        _tinkoffService = tinkoffService;
        _currencyService = currencyService;
        _steam = steam;
        _notion = notion;
        _db = db;
    }

    public async Task HandleCommandAsync(Message message)
    {
        var command = message.Text.Split(' ')[0].ToLower();

        switch (command)
        {
            //case "/dollarcourse":
            //    await SendCurrencyRate(message.Chat.Id, "USD");
            //    break;
            //case "/eurocourse":
            //    await SendCurrencyRate(message.Chat.Id, "EUR");
            //    break;
            case "/portfolio":
                await SendPortfolioInfo(message.Chat.Id);
                break;
            case "/favorites":
                await SendFavoritesInfo(message.Chat.Id);
                break;
            case "/searchbyticket":
                await RequestStockTicker(message.Chat.Id);
                break;
        }
    }

    public async Task HandleTextMessageAsync(Message message)
    {
        if (_awaitingTicker.TryGetValue(message.Chat.Id, out var stockAction) && stockAction == "stock")
        {
            _awaitingTicker.Remove(message.Chat.Id);
            await ProcessStockRequest(message.Chat.Id, message.Text.Trim().ToUpper());
        }
    }

    #region Private

    #region Valute

    private async Task SendCurrencyRate(long chatId, string currencyCode)
    {
        Console.WriteLine($"Getting valute was called: {currencyCode}");
        var rate = await _currencyService.GetValuteRateFormatAsync(currencyCode);
        await _botClient.SendMessage(chatId, rate, parseMode: ParseMode.Html);
    }


    #endregion

    #region Invests

    private async Task ProcessStockRequest(long chatId, string ticker)
    {
        var stock = await _tinkoffService.FindStockAsync(ticker);
        if (stock == null)
        {
            await _botClient.SendMessage(chatId, $"❌ Акция '{ticker}' не найдена");
            return;
        }

        var price = await _tinkoffService.GetStockPriceAsync(stock.Figi);
        var currency = stock.Currency == "rub" ? "RUB" : stock.Currency.ToUpper();

        await _botClient.SendMessage(
            chatId,
            $"📊 <b>{stock.Name} ({stock.Ticker})</b>\n" +
            $"💵 Цена: {price:N2} {currency}\n" +
            $"📆 Лот: {stock.Lot} акций",
            parseMode: ParseMode.Html);
    }

    private async Task RequestStockTicker(long chatId)
    {
        _awaitingTicker[chatId] = "stock";
        await _botClient.SendMessage(
            chatId,
            "🔍 Введите тикер акции (например: SBER, GAZP):",
            replyMarkup: new ForceReplyMarkup { Selective = true });
    }

    private async Task SendPortfolioInfo(long chatId)
    {
        try
        {
            var portfolioInfo = await _tinkoffService.GetPortfolioInfoAsync();
            await _botClient.SendMessage(
                chatId,
                $"📊 <b>Ваш портфель:</b>\n{portfolioInfo}",
                parseMode: ParseMode.Html);
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                chatId,
                $"⚠️ Ошибка при получении портфеля: {ex.Message}");
        }
    }

    private async Task SendFavoritesInfo(long chatId)
    {
        try
        {
            var (favoritesInfo, entities) = await _tinkoffService.GetFavoriteInstrumentsInfoAsync();
            await _botClient.SendMessage(
                chatId,
                favoritesInfo,
                parseMode: ParseMode.Html,
                entities: entities);
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                chatId,
                $"⚠️ Ошибка при получении избранных активов: {ex.Message}");
        }
    }

    
    #endregion

    #endregion


}