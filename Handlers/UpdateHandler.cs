using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Core.Interfaces;

namespace MyTestTelegramBot.Handlers
{
    public class UpdateHandler
    {
        private readonly ICommandService _commandService;
        private readonly ICurrencyService _currencyService;
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<UpdateHandler> _logger;

        public UpdateHandler(
            ICommandService commandService,
            ICurrencyService currencyService,
            IMessageHandler messageHandler,
            ILogger<UpdateHandler> logger)
        {
            _commandService = commandService;
            _currencyService = currencyService;
            _messageHandler = messageHandler;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var handler = update.Type switch
                {
                    UpdateType.Message => _messageHandler.HandleMessageAsync(botClient, update.Message!, cancellationToken),
                    UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken),
                    UpdateType.PollAnswer => HandlePollAnswerAsync(botClient, update.PollAnswer!, cancellationToken),
                    _ => Task.CompletedTask // add poll
                };

                await handler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task HandlePollAnswerAsync(ITelegramBotClient botClient, PollAnswer pollAnswer, CancellationToken cancellationToken)
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

                        await botClient.SendMessage(
                            pollAnswer.User.Id,
                            response,
                            parseMode: ParseMode.Html);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendMessage(
                            pollAnswer.User.Id,
                            $"⚠️ Ошибка при получении данных: {ex.Message}");
                    }
                }
                else
                {
                    var averageRate = await _currencyService.GetAverageUsdRateForYearAsync(year);
                    await botClient.SendMessage(
                        pollAnswer.User.Id,
                        $"📊 Средний курс USD за {year} год: {averageRate:F2} RUB",
                        parseMode: ParseMode.Html);
                }
            }
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            Console.WriteLine("This is CallBAck");
            var command = callbackQuery.Data switch // TODO: Вот это все в енамы
            {
                //Notion commands
                "notion_command" => "/notionmenu",
                "notion_add_mark_command" => "/notionaddmark",
                "notion_add_task_command" => "/notionaddtask",
                "notion_get_marks_command" => "/notiongetmarks",
                "notion_get_week_tasks_command" => "/notiongetweektasks",
                "notion_add_task_good_result_command" => "/notionaddtaskgood",
                "notion_add_task_bad_result_command" => "/notionaddtaskbad",
                "notion_add_task_bad_result_but_save_command" => "/notionaddtaskbadbutsave",

                //Steam commands
                "steam_command" => "/steammenu",
                "steam_addOneItem_command" => "/addonesteamtextdata",
                "steam_addManyItems_command" => "/addsteamexceldata",
                "steam_countData_command" => "/createsteampricetable",

                //Valute commands
                "valute_command" => "/avgyearusdcourseforthisyear",

                //Invest commands
                "invest_command" => "/portfolio",
                
                _ => null
            };

            if (command != null)
            {
                // Создаем фиктивное сообщение для выполнения команды
                var fakeMessage = new Message
                {
                    From = callbackQuery.From,
                    Chat = callbackQuery.Message.Chat,
                    Text = command,
                    Date = DateTime.UtcNow
                };

                await _commandService.ExecuteCommandAsync(botClient, fakeMessage, cancellationToken);
            }

            // Подтверждаем обработку callback (убираем "часики")
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);

            //await _commandService.ExecuteCallbackAsync(botClient, callbackQuery, cancellationToken);
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Polling error occurred");
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
