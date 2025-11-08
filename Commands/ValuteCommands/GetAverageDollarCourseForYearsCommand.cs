using Telegram.Bot.Types;
using Telegram.Bot;

namespace MyTestTelegramBot.Commands.ValuteCommands;

public class GetAverageDollarCourseForYearsCommand : BaseCommand
{
    public override string Name => "/avgyearusdcourseforthisyear";
    public override string Description => "avgyearusdcourseforthisyear";

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Command /avgyearusdcourseforthisyear was called by user {message.From?.Username}");

        var yearsArray = new List<InputPollOption>() {
            new InputPollOption() { Text = "Информация за 10 лет"},
            new InputPollOption() { Text = "2025"},
            new InputPollOption() { Text = "2024"},
            new InputPollOption() { Text = "2023"},
            new InputPollOption() { Text = "2022"},
            new InputPollOption() { Text = "2021"},
            new InputPollOption() { Text = "2020"},
        };

        await botClient.SendPoll(
                    message.Chat.Id,
                    "Выберите год:",
                    yearsArray,
                    isAnonymous: false);
    }
}
