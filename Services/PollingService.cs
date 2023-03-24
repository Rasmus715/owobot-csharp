using Microsoft.Extensions.Logging;
using owobot_csharp.Abstract;
using Telegram.Bot.Abstract;

namespace owobot_csharp.Services;

public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger<PollingServiceBase<ReceiverService>> logger, IReceiverService receiver)
        : base(serviceProvider, logger, receiver)
    {
    }
}