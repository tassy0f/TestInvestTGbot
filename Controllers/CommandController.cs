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
            //case "/dollarcourse":
            //    await SendCurrencyRate(message.Chat.Id, "USD");
            //    break;
            //case "/eurocourse":
            //    await SendCurrencyRate(message.Chat.Id, "EUR");
            //    break;
            case "/addnotiontask":
                await AddNotionTask(message.Chat.Id);
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
        if (_awaitingTicker.TryGetValue(message.Chat.Id, out var stockAction) && stockAction == "stock")
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

    #region Private

    private async Task SendStartMessage(long chatId)
    {
        Console.WriteLine("Command /start was called");
        var currencyList = await _currencyService.GetValuteRateListAsync(new string[] { "USD", "EUR" });
        //var dollarCurrency = await _currencyService.GetValuteRateAsync("USD");
        //var euroCurrency = await _currencyService.GetValuteRateAsync("EUR");
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton($"📊 Курс доллара: {currencyList[0]?.Value} ₽") },
            new[] { new KeyboardButton($"📊 Курс евро: {currencyList[1]?.Value} ₽") },
            new[] { new KeyboardButton("🔒 Регистрация (недоступно)") }
        })
        {
            ResizeKeyboard = true
        };

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"💵 Доллар: {currencyList[0]?.Value} ₽", "ignore_dollar"),
                InlineKeyboardButton.WithCallbackData($"💶 Евро: {currencyList[1]?.Value} ₽", "ignore_euro")
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
            /avgyearusdcourseforthisyear — USD за год

            <i>💵 Steam:</i>
            /steamMenu

            <i>💵 Notion:</i>
            /addnotiontask

            <i>⚙️ Акции:</i>
            /portfolio — Портфель
            /favorites — Избранные активы
            /searchbyticket — Поиск акции
            """,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard);
    }

    #region Notion

    private async Task AddNotionTask(long chatId)
    {
        _awaitingTicker[chatId] = "notionVoice";
        var dbId = "9a0ebbb0bbe340cc8848773bfa61dfca"; // https://www.notion.so/9a0ebbb0bbe340cc8848773bfa61dfca?v=d6a27963f83f4d83b158a53bc1a9fdbe 
        // Добавить вот тут на выбор две кнопки в которых будет храниться ДбАйди заметки - айдишник, ежедневник - айдищник
        await _notion.AddTaskAsync(
            dbId,
            "Сделать ревью PR",
            DateTime.Today.AddDays(1),
            "Не забыть проверить юнит-тесты"
        );

        await _botClient.SendMessage(chatId, "✅ Задача добавлена в Notion!");
    }

    #endregion

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