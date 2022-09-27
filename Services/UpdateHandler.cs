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
        var handler = BotOnMessageReceived(update.Message, cancellationToken);
        await handler;
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

        switch (message.Text!.ToLower().Split("@")[0])
        {
            case "owo":
                await _helperService.SendCustomMessage(message, "uwu", _botClient, cancellationToken);
                break;
            case "uwu":
                await _helperService.SendCustomMessage(message, "owo", _botClient, cancellationToken);
                break;
            case "/start":
                await _helperService.Start(message,_botClient, cancellationToken);
                break;
            case "/info":
                await _helperService.Info(message, _botClient, cancellationToken);
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
                async void RedditThread() => 
                    await _helperService.GetRandomPic(message, _botClient, cancellationToken);
                new Thread(RedditThread).Start();
                break;
            default:
                if (message.Text.Contains("/get_"))
                {
                    async void GetRedditThread() => 
                        await _helperService.GetPicFromReddit(message, _botClient, cancellationToken);
                    new Thread(GetRedditThread).Start();
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
}