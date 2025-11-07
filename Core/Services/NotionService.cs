using Microsoft.Extensions.Options;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Core.Models;
using MyTestTelegramBot.Core.Models.Settings;
using Notion.Client;
using System.Globalization;
using System.Text.RegularExpressions;
using DateTime = System.DateTime;

namespace MyTestTelegramBot.Core.Services;

public class NotionService : INotionService
{
    private readonly NotionSettings _settings;
    private readonly INotionClient _client;
    private readonly IRedisService _redisService;
    private string _taskDBId = "9a0ebbb0bbe340cc8848773bfa61dfca"; // https://www.notion.so/9a0ebbb0bbe340cc8848773bfa61dfca?v=d6a27963f83f4d83b158a53bc1a9fdbe 

    public NotionService(IOptions<NotionSettings> settings, IRedisService redisService)
    {
        _settings = settings.Value;
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = _settings.AuthToken
        });
        _redisService = redisService;
    }

    public async Task<bool> AddInternalNotionTask(NotionTaskModel model)
    {
        return await AddTaskAsync(_taskDBId, model.Title, model.Date, model.Description).ConfigureAwait(false);
    }

    public async Task<bool> AddTaskAsync(string databaseId, string title, DateTime date, string? note = null)
    {
        var properties = new Dictionary<string, PropertyValue>
        {
            ["Name"] = new TitlePropertyValue
            {
                Title = new List<RichTextBase>
                {
                    new RichTextText { Text = new Text { Content = title } }
                }
            },
            ["Date"] = new DatePropertyValue
            {
                Date = new Date() { Start = date, End = date },
            }
        };

        if (!string.IsNullOrEmpty(note))
        {
            properties["Description"] = new RichTextPropertyValue
            {
                RichText = new List<RichTextBase>
                {
                    new RichTextText { Text = new Text { Content = note } }
                }
            };
        }

        var result = await _client.Pages.CreateAsync(new PagesCreateParameters
        {
            Parent = new DatabaseParentInput() { DatabaseId = databaseId },
            Properties = properties
        });

        return result != null ? true : false;
    }

    //public NotionTaskModel ParseStringToNotionModel(string text)
    //{
    //    var model = new NotionTaskModel
    //    {
    //        Category = ExtractValue(text, @"Категори(?:я|и)[\s:,.]*([\p{L}\d\s\-]+?)(?=,?\s*(Заголов|Дата|Описан|$))"),
    //        Title = ExtractValue(text, @"Заголов(?:ок|ка)[\s:,.]*([\p{L}\d\s\-]+?)(?=,?\s*(Категори|Дата|Описан|$))"),
    //        Date = ParseDate(ExtractValue(text, @"Дата[\s:,.]*([\p{L}\d\s\-]+?)(?=,?\s*(Категори|Заголов|Описан|$))")),
    //        Description = ExtractValue(text, @"Описан(?:ие|ия)[\s:,.]*([\p{L}\d\s\-]+?)(?=,?\s*(Категори|Дата|Заголов|$))")
    //    };
    //    return model;
    //}

    public NotionTaskModel ParseStringToNotionModel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new NotionTaskModel();

        // Нормализуем пробелы и перевод строки в пробелы
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // Паттерн для поиска ключевых слов (поддерживаем простые вариации)
        var pattern = @"\b(Категори(?:я|и)?|Заголов(?:ок|ка)?|Дата|Описан(?:ие|ия)?)\b";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Если ключевые слова вообще не найдены - попытаемся распарсить простым способом
        if (matches.Count == 0)
        {
            // попробуем распарсить весь текст как заголовок/описание по запятой
            var parts = text.Split(new[] { ',' }, 2);
            return new NotionTaskModel
            {
                Category = string.Empty,
                Title = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                Date = DateTime.Today,
                Description = parts.Length > 1 ? parts[1].Trim() : string.Empty
            };
        }

        // Соберём словарь: нормализованное имя ключа -> значение (текст между ключами)
        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            int valueStart = m.Index + m.Length;
            int valueEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;

            var raw = text.Substring(valueStart, valueEnd - valueStart).Trim();

            // Обрезаем ведущие/хвостовые разделители (запятые, точки, двоеточия и пробелы)
            raw = raw.Trim().TrimStart(',', ':', '.', ' ').TrimEnd(',', ':', '.', ' ');

            // Нормализуем ключ (категория/заголовок/дата/описание)
            var key = NormalizeKey(m.Value);

            // Если несколько совпадений одного ключа — объединяем через пробел
            if (extracted.ContainsKey(key))
                extracted[key] = (extracted[key] + " " + raw).Trim();
            else
                extracted[key] = raw;
        }

        var model = new NotionTaskModel
        {
            Category = extracted.TryGetValue("category", out var cat) ? cat : string.Empty,
            Title = extracted.TryGetValue("title", out var t) ? t : string.Empty,
            Date = ParseDate(extracted.TryGetValue("date", out var d) ? d : string.Empty),
            Description = extracted.TryGetValue("description", out var desc) ? desc : string.Empty
        };

        return model;
    }

    private static string NormalizeKey(string rawKey)
    {
        rawKey = rawKey?.Trim().ToLowerInvariant() ?? string.Empty;

        if (rawKey.StartsWith("категор")) return "category";
        if (rawKey.StartsWith("заголов")) return "title";
        if (rawKey.StartsWith("дата")) return "date";
        if (rawKey.StartsWith("описан")) return "description";
        return rawKey;
    }

    private string ExtractValue(string input, string pattern)
    {
        var match = Regex.Match(input, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private DateTime ParseDate(string dateText)
    {
        if (string.IsNullOrWhiteSpace(dateText))
            return DateTime.Today;

        dateText = dateText.Trim().ToLower();

        // ✅ Простые фразы
        if (dateText == "сегодня") return DateTime.Today;
        if (dateText == "завтра") return DateTime.Today.AddDays(1);
        if (dateText == "послезавтра") return DateTime.Today.AddDays(2);

        // ✅ Относительные даты: "через неделю", "через 2 недели", "через месяц", "через 3 дня" и т.п.
        var relativeMatch = Regex.Match(dateText, @"через\s+(\d+)?\s*(д(ня|ней)?|недел(ю|и)|месяц(а|ев)?)");
        if (relativeMatch.Success)
        {
            int value = 1;
            if (int.TryParse(relativeMatch.Groups[1].Value, out var parsed))
                value = parsed;

            var unit = relativeMatch.Groups[3].Value;
            return unit switch
            {
                "дня" or "дней" or "д" => DateTime.Today.AddDays(value),
                "неделю" or "недели" or "недель" => DateTime.Today.AddDays(7 * value),
                "месяц" or "месяца" or "месяцев" => DateTime.Today.AddMonths(value),
                _ => DateTime.Today
            };
        }

        // ✅ Конкретные даты без года: "15 ноября", "2 декабря" и т.п.
        var monthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["января"] = 1,
            ["февраля"] = 2,
            ["марта"] = 3,
            ["апреля"] = 4,
            ["мая"] = 5,
            ["июня"] = 6,
            ["июля"] = 7,
            ["августа"] = 8,
            ["сентября"] = 9,
            ["октября"] = 10,
            ["ноября"] = 11,
            ["декабря"] = 12
        };

        var monthMatch = Regex.Match(dateText, @"(\d{1,2})\s+(января|февраля|марта|апреля|мая|июня|июля|августа|сентября|октября|ноября|декабря)");
        if (monthMatch.Success)
        {
            int day = int.Parse(monthMatch.Groups[1].Value);
            int month = monthNames[monthMatch.Groups[2].Value];
            int year = DateTime.Today.Year;

            // если дата уже прошла — переносим на следующий год
            var dateCandidate = new DateTime(year, month, day);
            //if (dateCandidate < DateTime.Today)
            //    dateCandidate = dateCandidate.AddYears(1);

            return dateCandidate;
        }

        // ✅ Попытка стандартного парсинга ("15.11", "02.12.2025" и т.д.)
        if (DateTime.TryParse(dateText, CultureInfo.GetCultureInfo("ru-RU"),
            DateTimeStyles.AssumeLocal, out var parsedDate))
            return parsedDate;

        // если ничего не подошло
        return DateTime.Today;
    }
}