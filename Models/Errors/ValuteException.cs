namespace MyTestTelegramBot.Models.Errors
{
    internal class ValuteException : Exception
    {
        public string Message {  get; set; } = string.Empty;
    }
}
