﻿using System.Diagnostics;
using System.Reflection;
using System.Resources;
using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;

var bot = new TelegramBotClient(Configuration.Token);

var me = await bot.GetMeAsync();

Console.Title = me.Username ?? "Owobot Jr.";

using var cts = new CancellationTokenSource();
var context = new ApplicationContext();

bot.StartReceiving(Handlers.HandleUpdateAsync,
    Handlers.HandleErrorAsync,
    new ReceiverOptions()
    {
        AllowedUpdates = Array.Empty<UpdateType>()
    },
    cts.Token);

Console.WriteLine($"Start listening for @{me.Username}");

Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();