using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTestTelegramBot.Core.Interfaces
{
    public interface ISteamService
    {
        Task<decimal?> GetCurrentPriceAsync(string itemName, int appId = 730, int currency = 5);
    }
}
