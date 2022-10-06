using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace owobot_csharp.Interfaces;

public interface IHelperService
{
    Task SendCustomMessage(Message message, string customMessage, ITelegramBotClient bot,
        CancellationToken cancellationToken);
    Task Start(Message message, ITelegramBotClient bot,
        CancellationToken cancellationToken);
    Task Info(Message message, ITelegramBotClient bot, CancellationToken cancellationToken);
    Task Status(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task LanguageInfo(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task GetStatus(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task NsfwStatus(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task GetRandomBooruPic(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task GetRandomBooruPic(ITelegramBotClient botClient, InlineQuery inlineQuery, 
        CancellationToken cancellationToken);
    Task GetRandomPic(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task GetPicFromReddit(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task SetLanguage(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task TurnNsfw(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task UnknownCommand(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
}