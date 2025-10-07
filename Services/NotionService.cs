using Notion.Client;
using DateTime = System.DateTime;

namespace MyTestTelegramBot.Services;

public class NotionService
{
    private readonly INotionClient _client;

    public NotionService(string authToken)
    {
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = authToken
        });
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
}