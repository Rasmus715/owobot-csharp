using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace owobot_csharp.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IHelperService _helperService;

    public UpdateHandler(ITelegramBotClient bot, IHelperService helperService)
    {
        _botClient = bot;
        _helperService = helperService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not null) 
            await BotOnMessageReceived(update.Message, cancellationToken);
        else
            await UnknownUpdateHandlerAsync();
    }

    private static Task UnknownUpdateHandlerAsync()
    { 
        return Task.CompletedTask;
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw new IndexOutOfRangeException();
    }

    private async Task BotOnMessageReceived(Message message,
        CancellationToken cancellationToken)
    {

        if (message.Type != MessageType.Text)
            return;

        await _helperService.SetUser(message, cancellationToken);
        await _helperService.SetChat(message, cancellationToken);
        await _helperService.WriteTotalRequests(await _helperService.ReadTotalRequests(cancellationToken), cancellationToken);

        switch (message.Text!.ToLower().Split("@")[0])
        {
            case "owo":
                _= _helperService.SendCustomMessage(message, "uwu", _botClient, cancellationToken);
                break;
            case "uwu":
                _ = _helperService.SendCustomMessage(message, "owo", _botClient, cancellationToken);
                break;
            case "/start":
                _ = _helperService.Start(message,_botClient, cancellationToken);
                break;
            case "/info":
                _ = _helperService.Info(message, _botClient, cancellationToken);
                break;
            case "/status":
                _ = _helperService.Status(message, _botClient, cancellationToken);
                break;
            case "/language":
                _ = _helperService.LanguageInfo(message, _botClient, cancellationToken);
                break;
            case "/get":
                _ = _helperService.GetStatus(message, _botClient, cancellationToken);
                break;
            case "/nsfw":
                _ = _helperService.NsfwStatus(message, _botClient, cancellationToken);
                break;
            case "/random":
                _ = _helperService.GetBooruPic(message, _botClient, cancellationToken);
                break;
            case "/random_reddit":
                _ = _helperService.GetRandomPic(message, _botClient, cancellationToken);
                break;
            default:
                if (message.Text.Contains("/get_"))
                {
                    _ = _helperService.GetPicFromReddit(message, _botClient, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/language"))
                {
                    _ = _helperService.SetLanguage(message, _botClient, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/nsfw"))
                {
                    _ = _helperService.TurnNsfw(message, _botClient, cancellationToken);
                }
                else
                {
                    _ = _helperService.UnknownCommand(message, _botClient, cancellationToken);
                }
                break;
        }
    }
}