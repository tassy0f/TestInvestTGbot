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
    private NotionTaskModel _notionTaskModel;
    private string _taskDBId = "9a0ebbb0bbe340cc8848773bfa61dfca"; // https://www.notion.so/9a0ebbb0bbe340cc8848773bfa61dfca?v=d6a27963f83f4d83b158a53bc1a9fdbe 

    public NotionService(IOptions<NotionSettings> settings)
    {
        _settings = settings.Value;
        _notionTaskModel = new NotionTaskModel();
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = _settings.AuthToken
        });
    }

    public async Task<bool> AddInternalNotionTask()
    {
        await AddTaskAsync(_taskDBId, _notionTaskModel.Title, _notionTaskModel.Date, _notionTaskModel.Description).ConfigureAwait(false);
        return true; // TODO: fixit
    }

    public async Task AddTaskAsync(string databaseId, string title, DateTime date, string? note = null)
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

        await _client.Pages.CreateAsync(new PagesCreateParameters
        {
            Parent = new DatabaseParentInput() { DatabaseId = databaseId },
            Properties = properties
        });
    }

    public NotionTaskModel ParseStringToNotionModel(string text)
    {
        var model = new NotionTaskModel
        {
            Title = ExtractValue(text, @"Заголовок\.\s*([^.]*)"),
            Description = ExtractValue(text, @"Описание\.\s*([^.]*)"),
            Date = ParseDate(ExtractValue(text, @"Дата\.\s*([^.]*)"))
        };
        _notionTaskModel = model;
        return _notionTaskModel;
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

        return dateText.ToLower() switch
        {
            "завтра" => DateTime.Today.AddDays(1),
            "сегодня" => DateTime.Today,
            "послезавтра" => DateTime.Today.AddDays(2),
            _ => DateTime.TryParse(dateText, CultureInfo.GetCultureInfo("ru-RU"),
                    DateTimeStyles.None, out var date)
                 ? date
                 : DateTime.Today
        };
    }
}