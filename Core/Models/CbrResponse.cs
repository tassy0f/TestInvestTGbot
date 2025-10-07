namespace MyTestTelegramBot.Core.Models
{
    public class CbrResponse
    {
        public Dictionary<string, Currency> Valute { get; set; }
    }

    public class Currency
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int Nominal { get; set; }  // Номинал (например, 1 USD = X RUB)
        public string CharCode { get; set; }  // Код валюты (USD, EUR и т.д.)
    }

    public class CbrDayResponse
    {
        public DateTime Date { get; set; }
        public Dictionary<string, Currency> Valute { get; set; }
    }
}
