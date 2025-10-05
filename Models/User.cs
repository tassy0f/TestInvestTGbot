namespace MyTestTelegramBot.Models
{
    public class User
    {
        public int Id { get; set; }          // PK
        public long ChatId { get; set; }     // Telegram ChatId
        public string? Username { get; set; }

        public ICollection<SteamHistoryDataItem> SteamHistory { get; set; } = new List<SteamHistoryDataItem>();
    }
}
