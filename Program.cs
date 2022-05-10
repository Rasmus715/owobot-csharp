using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

var bot = new TelegramBotClient(Configuration.Telegram.TelegramToken);

var me = await bot.GetMeAsync();

Console.Title = me.Username ?? "Owobot Jr.";

using var cts = new CancellationTokenSource();

bot.StartReceiving(Handlers.HandleUpdateAsync,
    Handlers.HandleErrorAsync,
    new ReceiverOptions()
    {
        ThrowPendingUpdates = true
    },
    cts.Token);

Console.WriteLine($"Start listening for @{me.Username}");

Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();