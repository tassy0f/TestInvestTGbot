namespace MyTestTelegramBot.Models
{
    public class SteamHistoryDataItem
    {
        public SteamHistoryDataItem() { }
        public SteamHistoryDataItem(string name, decimal pricePerUnit, int count, decimal priceForAll)
        {
            Name = name;
            PricePerUnit = pricePerUnit;
            Count = count;
            PriceForAll = priceForAll;
        }

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal PricePerUnit { get; set; }
        public int Count { get; set; }
        public decimal PriceForAll { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
