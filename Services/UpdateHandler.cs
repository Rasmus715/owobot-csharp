using Reddit.Things;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace owobot_csharp.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly IHelperService _helperService;
    public UpdateHandler(IHelperService helperService)
    {
        _helperService = helperService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await _helperService.SetChat(update.Message, cancellationToken);
        await _helperService.SetUser(update.Message, cancellationToken);

        if (update.Message is not null)
        {
            await _helperService.WriteTotalRequests(await _helperService.ReadTotalRequests(cancellationToken), cancellationToken);
            if (update.Message.Type.Equals(MessageType.Text))
            {
                await BotOnMessageReceived(update.Message, cancellationToken);
            }
            else 
                _ = _helperService.UnknownCommand(update.Message, cancellationToken);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw new IndexOutOfRangeException();
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        switch (message.Text!.ToLower().Split("@")[0])
        {
            case "owo":
                _ = _helperService.SendCustomMessage(message, "uwu", cancellationToken);
                break;
            case "uwu":
                _ = _helperService.SendCustomMessage(message, "owo", cancellationToken);
                break;
            case "/start":
                _ = _helperService.Start(message, cancellationToken);
                break;
            case "/info":
                _ = _helperService.Info(message,  cancellationToken);
                break;
            case "/status":
                _ = _helperService.Status(message, cancellationToken);
                break;
            case "/language":
                _ = _helperService.LanguageInfo(message, cancellationToken);
                break;
            case "/get":
                _ = _helperService.GetStatus(message, cancellationToken);
                break;
            case "/nsfw":
                _ = _helperService.NsfwStatus(message, cancellationToken);
                break;
            case "/random":
                _ = _helperService.GetBooruPic(message, cancellationToken);
                break;
            case "/random_reddit":
                _ = _helperService.GetRandomPic(message, cancellationToken);
                break;
            default:
                if (message.Text.Contains("/get_"))
                {
                    _ = _helperService.GetPicFromReddit(message, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/language"))
                {
                    await _helperService.SetLanguage(message, cancellationToken);
                    break;
                }

                if (message.Text.Contains("/nsfw"))
                {
                    await _helperService.TurnNsfw(message, cancellationToken);
                }
                else
                {
                    _ = _helperService.UnknownCommand(message, cancellationToken);
                }
                break;
        }
    }
}