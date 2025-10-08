using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class HandleVoiceMessageForNotionCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly INotionService _notionService;

    public HandleVoiceMessageForNotionCommand(IUserStateService stateService, INotionService notionService)
    {
        _stateService = stateService;
        _notionService = notionService;
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
}
