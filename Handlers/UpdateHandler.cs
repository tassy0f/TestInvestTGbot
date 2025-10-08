using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Core.Interfaces;

namespace MyTestTelegramBot.Handlers
{
    public class UpdateHandler
    {
        private readonly ICommandService _commandService;
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<UpdateHandler> _logger;

        public UpdateHandler(
            ICommandService commandService,
            IMessageHandler messageHandler,
            ILogger<UpdateHandler> logger)
        {
            _commandService = commandService;
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
                    _ => Task.CompletedTask // add poll
                };

                await handler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
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
