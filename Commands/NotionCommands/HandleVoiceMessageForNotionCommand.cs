using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MyTestTelegramBot.Data.Repository;
using MyTestTelegramBot.Core.Models;
using MyTestTelegramBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class HandleVoiceMessageForNotionCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;
    private readonly IRedisService _redisService;
    private readonly AppDbContext _db;

    public HandleVoiceMessageForNotionCommand(IUserStateService stateService, INotionService notionService, IRedisService redisService, AppDbContext db)
    {
        _stateService = stateService;
        _notionService = notionService;
        _redisService = redisService;
        _db = db;
    }

    public override string Name => "/handlevoicemessagefornotion";
    public override string Description => "Добавить файл для распознания";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingForNotionAudio };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var voice = message.Voice;
        var chatId = message.Chat.Id;

        var file = await botClient.GetFile(voice.FileId);
        var filePath = Path.Combine("voice", $"{voice.FileUniqueId}.ogg");

        Directory.CreateDirectory("voice");

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await botClient.DownloadFile(file.FilePath, fileStream);
        }

        await botClient.SendMessage(chatId, "Голосовое сообщение получено. Распознаю текст...");

        var recognizedText = await TranscribeAudioAsync(filePath);

        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            await botClient.SendMessage(chatId, "❌ Не удалось распознать речь.");
        }
        else
        {
            await botClient.SendMessage(chatId, $"Распознанный текст:\n{recognizedText}");
        }

        Console.WriteLine(recognizedText);

        var notionModelTask = _notionService.ParseStringToNotionModel(recognizedText);
        var prefixForTitle = await IncrementCategoryCountAsync(message.Chat.Username, notionModelTask.Category);
        notionModelTask.Title = prefixForTitle + char.ToUpper(notionModelTask.Title[0]) + notionModelTask.Title.Substring(1);
        await _redisService.SetNotionTaskAsync(chatId, notionModelTask);

        var askText = $"""
            <b>Проверьте, верна ли запись?</b>
            Заголовок: {notionModelTask.Title},
            Описание: {notionModelTask.Description},
            Дата: {notionModelTask.Date}.
           """;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Верно", "notion_add_task_good_result_command"),
                InlineKeyboardButton.WithCallbackData("Неверно", "notion_add_task_bad_result_command")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Неверно, но сохранить", "notion_add_task_bad_result_but_save_command")
            }
        });

        await botClient.SendMessage(
            chatId: chatId,
            text: askText,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        await _stateService.SetUserStateAsync(chatId, StateEnum.WaitingUserDesignByNotionModel);
    }

    private async Task<string> TranscribeAudioAsync(string audioFilePath) // TODO: Вынести это в WhisperService
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

    private async Task<string> IncrementCategoryCountAsync(string username, string categoryText)
    {
        var prefix = MapToPrefix(categoryText);

        // Получаем юзера
        var user = await _db.Users
                .Include(u => u.NotionCategories)
                .FirstOrDefaultAsync(u => u.Username == username);

        // Проверяем, есть ли категория
        var existing = await _db.NotionCategoryStats
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Prefix == prefix);

        if (existing == null)
        {
            existing = new NotionCategoryStats()
            {
                TaskCount = 1
            };
        }
        else
        {
            existing.TaskCount += 1;
        }

        return $"{prefix}-{existing.TaskCount} | ";
    }

    private string MapToPrefix(string categoryText)
    {
        categoryText = categoryText.ToLower().Trim();

        return categoryText switch
        {
            // Учёба
            var s when s.Contains("учеб") || s.Contains("сесс") => NotionPrefixs.StudiesSessions,

            // Рабочая учеба / компьютерные науки
            var s when s.Contains("работа") || s.Contains("програм") || s.Contains("айти") || s.Contains("комп") =>
                NotionPrefixs.ComputerScienceLearning,

            // Развлечения
            var s when s.Contains("развлеч") || s.Contains("отдых") || s.Contains("фан") =>
                NotionPrefixs.InRealLifeFun,

            // Законодательство / документы
            var s when s.Contains("закон") || s.Contains("налог") || s.Contains("паспорт") =>
                NotionPrefixs.InRealLifeLow,

            // Медицина
            var s when s.Contains("медиц") || s.Contains("здоров") || s.Contains("врач") =>
                NotionPrefixs.InRealLifeMedicine,

            // Дополнительная работа
            var s when s.Contains("подработка") || s.Contains("фриланс") =>
                NotionPrefixs.AdditionalWork,

            // Книги
            var s when s.Contains("чтение") || s.Contains("книга") => DetectBookCategory(categoryText),

            // По умолчанию — IRL (жизнь)
            _ => NotionPrefixs.InRealLife
        };
    }

    private string DetectBookCategory(string text)
    {
        if (text.Contains("айти") || text.Contains("работ"))
            return NotionPrefixs.ReadinBookIT;

        if (text.Contains("душ") || text.Contains("роман") || text.Contains("повесть"))
            return NotionPrefixs.ReadinBookArtistic;

        if (text.Contains("учеб") || text.Contains("образован"))
            return NotionPrefixs.ReadinBookEducation;

        return NotionPrefixs.ReadinBookArtistic;
    }
}
