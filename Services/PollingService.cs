using Microsoft.Extensions.Logging;
using Telegram.Bot.Abstract;

namespace owobot_csharp.Services;

public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : base(serviceProvider, logger)
    {
    }
}