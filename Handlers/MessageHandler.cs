using MyTestTelegramBot.Core.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using MyTestTelegramBot.Data.Repository;
using Microsoft.EntityFrameworkCore;
using User = MyTestTelegramBot.Data.Entities.User;

namespace MyTestTelegramBot.Handlers
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ICommandService _commandService;
        private readonly AppDbContext _db;
        private User _user;

        public MessageHandler(ICommandService commandService, AppDbContext db)
        {
            _commandService = commandService;
            _db = db;
            _user = new User();
        }

        public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var username = message.Chat.Username;
            if (_user.Username != username)
            {
                var user = await _db.Users
                .Include(u => u.SteamHistory)
                .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    var newUser = new User()
                    {
                        ChatId = message.Chat.Id,
                        Username = username,
                    };

                    _db.Users.Add(newUser);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    _user = user;
                }
            }
            

            if (message.Text is { } text)
            {
                if (text.StartsWith('/'))
                {
                    await _commandService.ExecuteCommandAsync(botClient, message, cancellationToken);
                }
                else
                {
                    await HandleRegularMessageAsync(botClient, message, cancellationToken);
                }
            }
            else if (message.Document is { } document)
            {
                Console.WriteLine(document.FileName);
                if (document.FileName.EndsWith(".xlsx"))
                {
                    await _commandService.ExecuteCommandAsync(botClient, message, cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        message.Chat.Id,
                        text: "Пожалуйста, загрузите Excel (.xlsx) файл."
                        );
                }
            }
            else if (message.Voice is { } voice)
            {
                await _commandService.ExecuteCommandAsync(botClient, message, cancellationToken);
            }
            else if (message.Poll is { } pollAnswer)
            {
                await _commandService.ExecuteCommandAsync(botClient, message, cancellationToken);
            }
        }

        private async Task HandleRegularMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            // Логика обработки обычных сообщений
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Сообщение получено",
                cancellationToken: cancellationToken);
        }
    }
}
