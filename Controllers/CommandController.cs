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
    private readonly Dictionary<long, string> _awaitingTicker = new();

    public CommandController(
        ITelegramBotClient botClient,
        TinkoffService tinkoffService,
        CurrencyService currencyService)
    {
        _botClient = botClient;
        _tinkoffService = tinkoffService;
        _currencyService = currencyService;
    }

    public async Task HandleCommandAsync(Message message)
    {
        var command = message.Text.Split(' ')[0].ToLower();
        var yearsArray = new List<InputPollOption>() {
            new InputPollOption() { Text = "Информация за 10 лет"},
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
                0 => 1,
                1 => 2025,
                2 => 2024,
                3 => 2023,
                4 => 2022,
                5 => 2021,
                6 => 2020,
                _ => DateTime.Now.Year
            };
            if (year == 1)
            {
                try
                {
                    var currentYear = DateTime.Now.Year;
                    var rates = new List<(int Year, decimal Rate)>();

                    // Получаем данные за последние 10 лет асинхронно
                    var tasks = Enumerable.Range(currentYear - 9, 10)
                        .Select(async year =>
                        {
                            var rate = await _currencyService.GetAverageUsdRateForYearAsync(year);
                            return (Year: year, Rate: rate);
                        })
                        .ToList();

                    await Task.WhenAll(tasks);
                    rates = tasks.Select(t => t.Result).ToList();

                    // Формируем красивую таблицу
                    var response = "📊 <b>Средние курсы USD за 10 лет</b>\n\n";
                    response += "<pre>";
                    response += "| Год   | Курс (RUB)  |\n";
                    response += "|-------|-------------|\n";

                    foreach (var item in rates.OrderByDescending(x => x.Year))
                    {
                        response += $"| {item.Year} | {item.Rate,10:N2} |\n";
                    }

                    response += "</pre>";
                    response += "\n<i>Данные предоставлены ЦБ РФ</i>";

                    await _botClient.SendMessage(
                        pollAnswer.User.Id,
                        response,
                        parseMode: ParseMode.Html);
                }
                catch (Exception ex)
                {
                    await _botClient.SendMessage(
                        pollAnswer.User.Id,
                        $"⚠️ Ошибка при получении данных: {ex.Message}");
                }
            } 
            else
            {
                var averageRate = await _currencyService.GetAverageUsdRateForYearAsync(year);
                await _botClient.SendMessage(
                    pollAnswer.User.Id,
                    $"📊 Средний курс USD за {year} год: {averageRate:F2} RUB",
                    parseMode: ParseMode.Html);
            }
        }
    }

    private async Task SendStartMessage(long chatId)
    {
        var dollarCurrency = await _currencyService.GetValuteRateAsync("USD");
        var euroCurrency = await _currencyService.GetValuteRateAsync("EUR");
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton($"📊 Курс доллара: {dollarCurrency?.Value} ₽") },
            new[] { new KeyboardButton($"📊 Курс евро: {euroCurrency?.Value} ₽") },
            new[] { new KeyboardButton("🔒 Регистрация (недоступно)") }
        })
        {
            ResizeKeyboard = true
        };

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"💵 Доллар: {dollarCurrency?.Value} ₽", "ignore_dollar"),
                InlineKeyboardButton.WithCallbackData($"💶 Евро: {euroCurrency?.Value} ₽", "ignore_euro")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔒 Регистрация", "register_disabled")
            }
        });
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
            /favorites — Избранные активы
            /searchbyticket — Поиск акции
            """,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard);
    }

    private async Task SendCurrencyRate(long chatId, string currencyCode)
    {
        var rate = await _currencyService.GetValuteRateFormatAsync(currencyCode);
        await _botClient.SendMessage(chatId, rate, parseMode: ParseMode.Html);
    }

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
}