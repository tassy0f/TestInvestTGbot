using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using MyTestTelegramBot.Data.Entities;
using MyTestTelegramBot.Data.Repository;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.NotionCommands;

public class HandleVoiceMessageForNotionCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly ISteamService _steamService;
    private readonly AppDbContext _db;

    public HandleVoiceMessageForNotionCommand(IUserStateService stateService, ISteamService steamService, AppDbContext db)
    {
        _stateService = stateService;
        _steamService = steamService;
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

        // TODO: добавить запись в Notion или создание задачи

        await _stateService.ClearUserStateAsync(message.From.Id);
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
