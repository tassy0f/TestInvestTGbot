using MyTestTelegramBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTestTelegramBot.Core.Interfaces
{
    public interface ICurrencyService
    {
        Task<Currency?> GetValuteRateAsync(string valuteCode);

        Task<List<Currency>?> GetValuteRateListAsync(string[] valuteArr);

        Task<string> GetValuteRateFormatAsync(string valuteCode);

        Task<decimal> GetAverageUsdRateForYearAsync(int year);
    }
}
