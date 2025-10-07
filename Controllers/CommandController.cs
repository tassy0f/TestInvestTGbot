using Microsoft.EntityFrameworkCore;
using MyTestTelegramBot.Models;
using MyTestTelegramBot.Models.DBContext;
using MyTestTelegramBot.Services;
using OfficeOpenXml;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = MyTestTelegramBot.Models.User;

namespace MyTestTelegramBot.Controllers;

public class CommandController
{
    private readonly ITelegramBotClient _botClient;
    private readonly TinkoffService _tinkoffService;
    private readonly CurrencyService _currencyService;
    private readonly SteamService _steam;
    private readonly NotionService _notion;
    private readonly AppDbContext _db;
    private readonly Dictionary<long, string> _awaitingTicker = new();
    private static readonly Dictionary<long, string> _userStates = new();

    public CommandController(
        ITelegramBotClient botClient,
        TinkoffService tinkoffService,
        CurrencyService currencyService,
        SteamService steam,
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
            case "/steammenu":
                await SendSteamMenu(message.Chat.Id);
                break;
            case "/uploadsteamdatahistory":
                await UploadSteamInventory(message.Chat.Id, message.Chat.Username);
                break;
            case "/generatecostform":
                await GenerateCostForm(message.Chat.Id, message.Chat.Username);
                break;
            case "/addsteamitem":
                await AddSteamItem(message.Chat.Id, message.Chat.Username);
                break;
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

        if (_awaitingTicker.TryGetValue(message.Chat.Id, out var steamAction) && steamAction == "steamitem")
        {
            _awaitingTicker.Remove(message.Chat.Id);
            await ProcessSteamRequest(message.Chat.Id, message.Text, message.Chat.Username);
        }
    }

    public async Task HandleVoiceMessageAsync(Message message)
    {
        if (!_awaitingTicker.TryGetValue(message.Chat.Id, out var notionVoiceAction) && notionVoiceAction == "notionVoice")
        {
            return;
        }
        var voice = message.Voice;
        var chatId = message.Chat.Id;

        var file = await _botClient.GetFile(voice.FileId);
        var filePath = Path.Combine("voice", $"{voice.FileUniqueId}.ogg");

        Directory.CreateDirectory("voice");

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await _botClient.DownloadFile(file.FilePath, fileStream);
        }

        await _botClient.SendMessage(chatId, "Голосовое сообщение получено. Распознаю текст...");

        var recognizedText = await TranscribeAudioAsync(filePath);

        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            await _botClient.SendMessage(chatId, "❌ Не удалось распознать речь.");
        }
        else
        {
            await _botClient.SendMessage(chatId, $"Распознанный текст:\n{recognizedText}");
        }

        Console.WriteLine(recognizedText);

        // TODO: добавить запись в Notion или создание задачи
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

    public async Task HandleXlsxDocumentAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var document = message.Document;
        var userName = message.From.Username;
        if (!_userStates.TryGetValue(chatId, out var state) || state != "waiting_excel")
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Сначала выбери команду /uploadsteamdatahistory, чтобы загрузить Excel\n" +
                "Названия элементов должны быть строго на английском!"
            );
            return;
        }

        var user = _db.Users.FirstOrDefault(u => u.Username == userName);
        if (user == null)
        {
            user = new User { ChatId = chatId, Username = userName };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        _userStates[chatId] = "noFlag";

        var file = await _botClient.GetFile(document.FileId);
        var pathFile = Path.Combine("uploadsFiles", document.FileName);
        Directory.CreateDirectory("uploadsFiles");

        using (var fileStream = new FileStream(pathFile, FileMode.Create))
        {
            await _botClient.DownloadFile(file.FilePath, fileStream);
        }

        await _botClient.SendMessage(
            chatId,
            text: $"Файл {document.FileName} успещно загружен"
            );
        var parsedModel = new List<SteamHistoryDataItem>();

        try
        {
            parsedModel = await ParseExcelToDBModel(pathFile, chatId);
        }
        catch (InvalidDataException ex)
        {
            await _botClient.SendMessage(
            chatId,
            text: $"Error: {ex.Message}"
            );
        }

        foreach (var item in parsedModel)
        {
            item.UserId = user.Id;
            Console.WriteLine(item.Name);
            Console.WriteLine(item.UserId);
            _db.SteamHistoryData.Add(item);
        }

        await _db.SaveChangesAsync();
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

    private async Task<string> TranscribeAudioAsync(string audioFilePath)
    {
        using var httpClient = new HttpClient();
        using var form = new MultipartFormDataContent();

        using var fileStream = File.OpenRead(audioFilePath);
        form.Add(new StreamContent(fileStream), "audio_file", Path.GetFileName(audioFilePath));

        var url = "http://localhost:9000/asr?task=transcribe&language=ru";

        var response = await httpClient.PostAsync(url, form);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Whisper error: {response.StatusCode}");
            return string.Empty;
        }

        var result = await response.Content.ReadAsStringAsync();

        return result ?? string.Empty;
    }

    #endregion

    #region Steam

    private async Task AddSteamItem(long id, string? username)
    {
        await _botClient.SendMessage(
            chatId: id,
            text: "Скопируйте сообщение ниже и отправтье боту заполненый шаблон (Название строго на английском):"
        );

        await _botClient.SendMessage(
            chatId: id,
            text: "Название:               \n"+
                  "Стоимость за единицу:   \n"+
                  "Количество:             \n"+
                  "Общая стоимость:        \n"
        );

        _awaitingTicker[id] = "steamitem";
    }

    private async Task ProcessSteamRequest(long id, string? text, string username)
    {
        var user = await _db.Users
        .Include(u => u.SteamHistory)
        .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            var newUser = new User()
            {
                ChatId = id,
                Username = username,
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            var userOneMoreTime = await _db.Users // TODO: вот это все хуйня, надо переделать
        .Include(u => u.SteamHistory)
        .FirstOrDefaultAsync(u => u.Username == username);

            // TODO: Добавить проверку на ошибку
            var newSteamItem = ParseFromMessage(text); // TODO: вынести это отсюда
            newSteamItem.UserId = userOneMoreTime.Id;
            _db.SteamHistoryData.Add(newSteamItem);
            await _db.SaveChangesAsync();
            _awaitingTicker.Remove(id);
        }
        else
        {
            var newSteamItem = ParseFromMessage(text);
            newSteamItem.UserId = user.Id;
            _db.SteamHistoryData.Add(newSteamItem); // TODO: в отдельный переиспользуемый метод
            await _db.SaveChangesAsync();
            _awaitingTicker.Remove(id);
        }
    }

    private SteamHistoryDataItem ParseFromMessage(string message)
    {
        var item = new SteamHistoryDataItem();
        var culture = CultureInfo.InvariantCulture;

        var nameMatch = Regex.Match(message, @"Название:\s*(.+)");
        var pricePerUnitMatch = Regex.Match(message, @"Стоимость за единицу:\s*([\d.,]+)");
        var countMatch = Regex.Match(message, @"Количество:\s*(\d+)");
        var totalPriceMatch = Regex.Match(message, @"Общая стоимость:\s*([\d.,]+)");

        if (nameMatch.Success)
            item.Name = nameMatch.Groups[1].Value.Trim();

        if (pricePerUnitMatch.Success &&
            decimal.TryParse(pricePerUnitMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, culture, out var pricePerUnit))
            item.PricePerUnit = pricePerUnit;

        if (countMatch.Success &&
            int.TryParse(countMatch.Groups[1].Value, out var count))
            item.Count = count;

        if (totalPriceMatch.Success &&
            decimal.TryParse(totalPriceMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, culture, out var total))
            item.PriceForAll = total;

        return item;
    }

    private async Task GenerateCostForm(long id, string? username)
    {
        var user = await _db.Users
        .Include(u => u.SteamHistory)
        .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !user.SteamHistory.Any())
        {
            await _botClient.SendMessage(id, "Нет данных по твоим покупкам. Сначала загрузи Excel через /uploadsteamdatahistory");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("📊 Сравнение цен:");
        var unitedResult = UniteSteamDataItems(user.SteamHistory);

        foreach (var item in unitedResult)
        {
            var currentPrice = await _steam.GetCurrentPriceAsync(item.Name);
            if (currentPrice != null)
            {
                var diff = currentPrice.Value - item.PricePerUnit;
                string diffEmoji = diff < 0 ? "🔻" : diff > 0 ? "🟩" : "⚪";
                sb.AppendLine($"{item.Name}\n Куплено за: {item.PricePerUnit}₽ | Сейчас: {currentPrice}₽ | {diffEmoji} {diff:+0.00;-0.00}₽\n");
            }
            else
            {
                sb.AppendLine($"{item.Name} → ❌ Не удалось получить цену");
            }
        }

        await _botClient.SendMessage(id, sb.ToString());
    }

    private List<SteamHistoryDataItem> UniteSteamDataItems(ICollection<SteamHistoryDataItem> steamHistory)
    {
        return steamHistory
            .GroupBy(s => s.Name)
            .Select(g => new SteamHistoryDataItem
            {
                Name = g.Key,
                Count = g.Sum(x => x.Count),
                PriceForAll = g.Sum(x => x.PriceForAll),
                PricePerUnit = g.Sum(x => x.PriceForAll) / g.Sum(x => x.Count)
            })
            .ToList();
    }

    private async Task UploadSteamInventory(long id, string? username)
    {
        _userStates[id] = "waiting_excel";
        await _botClient.SendMessage(
            chatId: id,
            text: "Окей ✅ Теперь пришли мне Excel (.xlsx) файл с историей покупок наклеек\n" +
            "Названия элементов должны быть строго на английском!"
        );
    }

    private async Task<List<SteamHistoryDataItem>> ParseExcelToDBModel(string filePath, long chatId)
    {
        ExcelPackage.License.SetNonCommercialPersonal("myLicesense");

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0]; // первая вкладка

        int rowCount = worksheet.Dimension.Rows;

        var items = new List<SteamHistoryDataItem>();

        var russianLettersRegex = new Regex(@"[а-яА-ЯёЁ]", RegexOptions.Compiled);

        for (int row = 2; row <= rowCount; row++) // начиная со 2 строки (первая — заголовки)
        {
            string name = worksheet.Cells[row, 1].Text.Trim();

            if (russianLettersRegex.IsMatch(name))
            {
                throw new InvalidDataException($"⚠️ В строке {row} обнаружены русские символы в названии: \"{name}\"");
            }

            decimal pricePerUnit = Convert.ToDecimal(worksheet.Cells[row, 3].Text);
            int count = Convert.ToInt32(worksheet.Cells[row, 2].Text);
            decimal priceForAll = Convert.ToDecimal(worksheet.Cells[row, 4].Text);

            var steamItem = new SteamHistoryDataItem(name, pricePerUnit, count, priceForAll);
            items.Add(steamItem);
        }

        var result = string.Join("\n", items);

        await _botClient.SendMessage(
            chatId: chatId,
            text: $"Нашёл {rowCount - 1} записей:\n\n{result}"
        );
        return items;
    }

    private async Task SendSteamMenu(long id)
    {
        await _botClient.SendMessage(
            id,
            """
            <i>Загрузить данные по покупке инвентаря:</i>
            /uploadSteamDataHistory

            <i>Сформировать таблицу стоимости:</i>
            /generateCostForm

            <i>Загрузить одну единицу инвентаря:</i>
            /addsteamitem
            """,
            parseMode: ParseMode.Html);
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