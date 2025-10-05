using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTestTelegramBot.Models.Settings;

internal class PostgressSettings
{
    public string Host {  get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
