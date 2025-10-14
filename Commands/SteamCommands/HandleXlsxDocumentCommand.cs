using MyTestTelegramBot.Core.Common.States;
using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Data.Repository;
using MyTestTelegramBot.Data.Entities;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace MyTestTelegramBot.Commands.SteamCommands;

public class HandleXlsxDocumentCommand : BaseCommand
{
    private readonly IUserStateService _stateService;
    private readonly ISteamService _steamService;
    private readonly AppDbContext _db;

    public HandleXlsxDocumentCommand(IUserStateService stateService, ISteamService steamService, AppDbContext db)
    {
        _stateService = stateService;
        _steamService = steamService;
        _db = db;
    }

    public override string Name => "/handlexlsxdocumentsteamdata";
    public override string Description => "Добавить файл для распознания";
    public override bool CheckState => true;
    public override string[] RequiredStates => new[] { StateEnum.WaitingForSteamExcelFile };

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var id = message.Chat.Id;
        var document = message.Document;
        var userName = message.From.Username;

        var user = _db.Users.FirstOrDefault(u => u.Username == userName);

        var file = await botClient.GetFile(document.FileId);
        var pathFile = Path.Combine("uploadsFiles", document.FileName);
        Directory.CreateDirectory("uploadsFiles");

        using (var fileStream = new FileStream(pathFile, FileMode.Create))
        {
            await botClient.DownloadFile(file.FilePath, fileStream);
        }

        await botClient.SendMessage(
            id,
            text: $"Файл {document.FileName} успешно загружен"
        );
        var parsedModel = new List<SteamHistoryDataItem>();

        try
        {
            parsedModel = await ParseExcelToDBModel(pathFile, id);

            await botClient.SendMessage(
                chatId: id,
                text: $"Нашёл {parsedModel.Count} записей."
            );
        }
        catch (InvalidDataException ex)
        {
            await botClient.SendMessage(
            id,
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

        await _stateService.ClearUserStateAsync(message.From.Id);
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

        return items;
    }
}
