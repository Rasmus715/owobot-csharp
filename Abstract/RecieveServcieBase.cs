using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace owobot_csharp.Abstract;

/// <summary>
/// An abstract class to compose Receiver Service and Update Handler classes
/// </summary>
/// <typeparam name="TUpdateHandler">Update Handler to use in Update Receiver</typeparam>
public abstract class ReceiverServiceBase<TUpdateHandler> : IReceiverService
    where TUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandlers;
    private readonly ILogger<ReceiverServiceBase<TUpdateHandler>> _logger;

    protected ReceiverServiceBase(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<TUpdateHandler>> logger)
    {
        _botClient = botClient;
        _updateHandlers = updateHandler;
        _logger = logger;
    }

    /// <summary>
    /// Start to service Updates with provided Update Handler class
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        // ToDo: we can inject ReceiverOptions through IOptions container
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true
        };

        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Start receiving updates for @{BotName}", me.Username ?? "Owobot");

        // Start receiving updates
        await _botClient.ReceiveAsync(_updateHandlers, receiverOptions, stoppingToken);
    }
}
