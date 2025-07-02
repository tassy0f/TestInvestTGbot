using MyTestTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTestTelegramBot.Controllers;

public class CommandController
{
    private readonly ITelegramBotClient _botClient;
    private readonly TinkoffService _tinkoffService;
    private readonly CurrencyService _currencyService;
    private readonly StockService _stockService;
    private readonly Dictionary<long, string> _awaitingTicker = new();

    public CommandController(
        ITelegramBotClient botClient,
        TinkoffService tinkoffService,
        CurrencyService currencyService,
        StockService stockService)
    {
        _botClient = botClient;
        _tinkoffService = tinkoffService;
        _currencyService = currencyService;
        _stockService = stockService;
    }

    public async Task HandleCommandAsync(Message message)
    {
        var command = message.Text.Split(' ')[0].ToLower();
        var yearsArray = new List<InputPollOption>() {
            new InputPollOption() { Text = "2025"},
            new InputPollOption() { Text = "2024"},
            new InputPollOption() { Text = "2023"},
            new InputPollOption() { Text = "2022"},
            new InputPollOption() { Text = "2021"},
            new InputPollOption() { Text = "2020"},
        };
        switch (command)
        {
            case "/start":
                await SendStartMessage(message.Chat.Id);
                break;
            case "/dollarcourse":
                await SendCurrencyRate(message.Chat.Id, "USD");
                break;
            case "/eurocourse":
                await SendCurrencyRate(message.Chat.Id, "EUR");
                break;
            case "/avgyearusdcourseforthisyear":
                await _botClient.SendPoll(
                    message.Chat.Id,
                    "Выберите год:",
                    yearsArray,
                    isAnonymous: false);
                break;
            case "/portfolio":
                await SendPortfolioInfo(message.Chat.Id);
                break;
            case "/searchbyticket":
                await RequestStockTicker(message.Chat.Id);
                break;
        }
    }

    public async Task HandleTextMessageAsync(Message message)
    {
        if (_awaitingTicker.TryGetValue(message.Chat.Id, out var action) && action == "stock")
        {
            _awaitingTicker.Remove(message.Chat.Id);
            await ProcessStockRequest(message.Chat.Id, message.Text.Trim().ToUpper());
        }
    }

    public async Task HandlePollAnswerAsync(PollAnswer pollAnswer)
    {
        if (pollAnswer.User != null)
        {
            int year = pollAnswer.OptionIds[0] switch
            {
                0 => 2025,
                1 => 2024,
                2 => 2023,
                3 => 2022,
                4 => 2021,
                5 => 2020,
                _ => DateTime.Now.Year
            };

            var averageRate = await _currencyService.GetAverageUsdRateForYearAsync(year);
            await _botClient.SendMessage(
                pollAnswer.User.Id,
                $"📊 Средний курс USD за {year} год: {averageRate:F2} RUB",
                parseMode: ParseMode.Html);
        }
    }

    private async Task SendStartMessage(long chatId)
    {
        await _botClient.SendMessage(
            chatId,
            """
            <b><u>📊 Опции бота</u></b>

            <i>💵 Курсы валют:</i>
            /dollarcourse — USD
            /eurocourse — EUR
            /avgyearusdcourseforthisyear — USD за год

            <i>⚙️ Акции:</i>
            /portfolio — Портфель
            /searchbyticket — Поиск акции
            """,
            parseMode: ParseMode.Html);
    }

    private async Task SendCurrencyRate(long chatId, string currencyCode)
    {
        var rate = await _currencyService.GetValuteRateAsync(currencyCode);
        await _botClient.SendMessage(chatId, rate, parseMode: ParseMode.Html);
    }

    private async Task ProcessStockRequest(long chatId, string ticker)
    {
        var stock = await _stockService.FindStockAsync(ticker);
        if (stock == null)
        {
            await _botClient.SendMessage(chatId, $"❌ Акция '{ticker}' не найдена");
            return;
        }

        var price = await _stockService.GetStockPriceAsync(stock.Figi);
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
}