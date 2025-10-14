namespace MyTestTelegramBot.Core.Common.States;

public static class StateEnum
{
    public const string MainMenu = "main_menu";
    public const string WaitingForSteamExcelFile = "waiting_for_staem_excel_file";
    public const string WaitingForOneSteamFile = "waiting_for_on_steam_text_file";
    public const string WaitingForNotionAudio = "waiting_for_notion_audio";
    public const string WaitingUserDesignByNotionModel = "waiting_user_for_accept_notion_model";
}
