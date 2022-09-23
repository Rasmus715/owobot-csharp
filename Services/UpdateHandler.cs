using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace owobot_csharp.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IHelperService _helperService;

    public UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, IHelperService helperService)
    {
        _botClient = bot;
        _logger = logger;
        _helperService = helperService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            {Message: { } message} => BotOnMessageReceived(message, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update)
        };

        await handler;
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw new IndexOutOfRangeException();
    }

    private async Task BotOnMessageReceived(Message message,
        CancellationToken cancellationToken)
    {
        
        //Console.WriteLine(Resources.Handlers.Handlers_HandleUpdateAsync_, message.Type, message.From?.Id, message.Text);

        if (message.Type != MessageType.Text)
            return;

        switch (message.Text!.ToLower().Split("@")[0])
        {
            case "owo":
                await _helperService.SendCustomMessage(message, "uwu", _botClient, cancellationToken);
                break;
            case "uwu":
                await _helperService.SendCustomMessage(message, "owo", _botClient, cancellationToken);
                break;
            case "/start":
                await _helperService.Start(_botClient, message, cancellationToken);
                break;
            case "/info":
                await _helperService.Info(_botClient, message, cancellationToken);
                break;
            case "/status":
                await _helperService.Status(message, _botClient, cancellationToken);
                break;
            case "/language":
                await _helperService.LanguageInfo(message, _botClient, cancellationToken);
                break;
            case "/get":
                await _helperService.GetStatus(message, _botClient, cancellationToken);
                break;
            case "/nsfw":
                await _helperService.NsfwStatus(message, _botClient, cancellationToken);
                break;
            case "/random":

                async void BooruThread() =>
                    await _helperService.GetRandomBooruPic(message, _botClient, cancellationToken);

                new Thread(BooruThread).Start();
                break;
            case "/random_reddit":
                async void RedditThread() => await _helperService.GetRandomPic(message, _botClient, cancellationToken);
                new Thread(RedditThread).Start();
                break;
            default:
                if (message.Text.Contains("/get_"))
                {
                    await _helperService.GetPicFromReddit(message, _botClient, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/language"))
                {
                    await _helperService.SetLanguage(message, _botClient, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/nsfw"))
                {
                    await _helperService.TurnNsfw(message, _botClient, cancellationToken);
                }
                else
                {
                    await _helperService.UnknownCommand(message, _botClient, cancellationToken);
                }

                break;
        }
    }
    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
    
}