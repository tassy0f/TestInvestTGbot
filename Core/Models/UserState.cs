namespace MyTestTelegramBot.Core.Models;

public class UserState
{
    public long UserId { get; set; }
    public string CurrentState { get; set; } = "main_menu";
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
