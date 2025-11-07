namespace MyTestTelegramBot.Data.Entities;

public class NotionCategoryStats
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Prefix { get; set; } = null!;

    public int TaskCount { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
}
