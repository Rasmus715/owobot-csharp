using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;


var bot = new TelegramBotClient(Configuration.Telegram.TelegramToken);

Console.WriteLine("Initializing migration...");
var applicationContext = new ApplicationContext();
applicationContext.Database.Migrate();
applicationContext.Dispose();
Console.WriteLine("Migration successful");


var me = await bot.GetMeAsync();


    Console.WriteLine($"Start listening for @{me.Username}");

await bot.ReceiveAsync(Handlers.HandleUpdateAsync,
    Handlers.HandleErrorAsync,
    new ReceiverOptions()
    {
        ThrowPendingUpdates = true
    });

