using Microsoft.Extensions.Logging;
using owobot_csharp.Abstract;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace owobot_csharp.Services;

public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}