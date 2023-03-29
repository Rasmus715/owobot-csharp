#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using BooruSharp.Booru;
using BooruSharp.Search.Post;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using owobot_csharp.Data;
using owobot_csharp.Enums.Subreddits;
using owobot_csharp.Exceptions;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.IO.File;
using Chat = owobot_csharp.Models.Chat;
using User = owobot_csharp.Models.User;

namespace owobot_csharp.Services;

public interface IHelperService
{
    Task SendCustomMessage(Message message, string customMessage, CancellationToken cancellationToken);
    Task Start(Message message, CancellationToken cancellationToken);
    Task Info(Message message, CancellationToken cancellationToken);
    Task Status(Message message, CancellationToken cancellationToken);
    Task LanguageInfo(Message message, CancellationToken cancellationToken);
    Task GetStatus(Message message, CancellationToken cancellationToken);
    Task NsfwStatus(Message message, CancellationToken cancellationToken);
    Task GetRandomPic(Message message, CancellationToken cancellationToken);
    Task GetPicFromReddit(Message message, CancellationToken cancellationToken);
    Task SetLanguage(Message message, CancellationToken cancellationToken);
    Task TurnNsfw(Message message, CancellationToken cancellationToken);
    Task UnknownCommand(Message message, CancellationToken cancellationToken);
    Task SetUser(Message message, CancellationToken cancellationToken);
    Task SetChat(Message message, CancellationToken cancellationToken);
    Task WriteTotalRequests(int requests, CancellationToken cancellationToken);
    Task<int> ReadTotalRequests(CancellationToken cancellationToken);
    Task GetBooruPic(Message message,  CancellationToken cancellationToken);
}

public class HelperService : IHelperService
{
    private const string TotalRequestsPath = "Essentials/TotalRequests.txt";

    private readonly ILogger<HelperService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConfiguration _configuration;
    private readonly ApplicationContext _context;
    private readonly ResourceManager _resourceManager;
    private User _user;
    private Chat? _chat;

    public HelperService(ILogger<HelperService> logger, 
        ApplicationContext context, 
        ITelegramBotClient telegramBotClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
           Assembly.GetExecutingAssembly());
        _botClient = telegramBotClient;
        _configuration = configuration;
        // To suppress compiler warning. This prop cannot be null anyway.
        _user = new User();
    }

    public async Task<User> GetUser(Message message, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(
            c => c.Id == message.From!.Id,
            cancellationToken) ?? await RegisterUser(message, cancellationToken);
    }

    public async Task SetUser(Message message, CancellationToken cancellationToken)
    {
        _user = await _context.Users.FirstOrDefaultAsync(
            c => c.Id == message.From!.Id,
            cancellationToken) ?? await RegisterUser(message, cancellationToken);
    }

    private async Task<User> RegisterUser(Message message, CancellationToken cancellationToken)
    {
        var entity = new User
        {
            Id = message.From!.Id,
            Language = "en-US"
        };

        await _context.Users.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added user {userId} to DB", message.From!.Id);
        return entity;
    }

    public async Task SetChat(Message message, CancellationToken cancellationToken)
    {
        _chat = message.Chat.Id < 0
            ? await _context.Chats.FirstOrDefaultAsync(c => c.Id == message.Chat.Id,
                cancellationToken) ?? await RegisterChat(message, cancellationToken)
            : null;
    }

    private async Task<Chat> RegisterChat(Message message, CancellationToken cancellationToken)
    {
        var entity = new Chat
        {
            Id = message.Chat.Id
        };

        await _context.Chats.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added chat {chatId} to DB", message.Chat.Id);
        return entity;
    }

    public async Task WriteTotalRequests(int requests, CancellationToken cancellationToken)
    {
        requests++;
        await WriteAllTextAsync(TotalRequestsPath, requests.ToString(), cancellationToken);
    }

    public async Task<int> ReadTotalRequests(CancellationToken cancellationToken)
    {
        try
        {
            return int.Parse(await ReadAllTextAsync(TotalRequestsPath, cancellationToken));
        }
        catch (Exception)
        {
            await WriteTotalRequests(0, cancellationToken);
            return 0;
        }
    }

    public async Task SendCustomMessage(Message message, string customResponse,
        CancellationToken cancellationToken)
    {
        await SendResponse(message, customResponse, cancellationToken);
    }

    public async Task Start(Message message, CancellationToken cancellationToken)
    {
        if (_chat is not null && message.Text!.Equals($"/start@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
            await SendResponse(message, string.Format(
                               _resourceManager.GetString("Start",
                                   CultureInfo.GetCultureInfo(_user.Language))!,
                               $"@{message.From?.Username ?? "User"}"), cancellationToken);
        else
            await SendResponse(message, string.Format(
                              _resourceManager.GetString("Start",
                                  CultureInfo.GetCultureInfo(_user.Language))!,
                              message.From?.Username ?? "User"), cancellationToken);
    }

    public async Task Info(Message message, CancellationToken cancellationToken)
    {
        if (_chat is not null && message.Text!.Equals($"/start@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
            await SendResponse(message, string.Format(
                        _resourceManager.GetString("Info_Chat",
                            CultureInfo.GetCultureInfo(_user.Language))!,
                        $"@{message.From?.Username}", _configuration.GetSection("BOT_VERSION").Value,
                        $"@{_botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
       else
            await SendResponse(message, string.Format(
                _resourceManager.GetString("Info",
                CultureInfo.GetCultureInfo(_user.Language))!,
                _configuration.GetSection("BOT_VERSION").Value), cancellationToken);
    }

    public async Task Status(Message message, CancellationToken cancellationToken)
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        if (_chat is not null && message.Text!.Equals($"/start@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
            _ = SendResponse(message, string.Format(_resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(_user.Language))!,
                uptime.Days,
                $"{uptime:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                string.Format(_resourceManager.GetString(_chat.Nsfw ? "OnSwitch" : "OffSwitch",
                        CultureInfo.GetCultureInfo(_user.Language))!),
                _configuration.GetSection("BOT_VERSION").Value), cancellationToken);
        else
            _ = SendResponse(message, string.Format(_resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(_user.Language))!,
                uptime.Days,
                $"{uptime:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                string.Format(_resourceManager.GetString(_user.Nsfw ? "OnSwitch" : "OffSwitch",
                        CultureInfo.GetCultureInfo(_user.Language))!),
                _configuration.GetSection("BOT_VERSION").Value), cancellationToken);
    }

    public async Task LanguageInfo(Message message, CancellationToken cancellationToken)
    {
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/language@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    await SendResponse(message, string.Format(_resourceManager.GetString("LanguageInfo_Chat",
                            CultureInfo.GetCultureInfo(_user.Language))!,
                        $"@{message.From?.Username}",
                        $"@{_botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
                break;
            case > 0:
            {
                _ = SendResponse(message, string.Format(_resourceManager.GetString("LanguageInfo",
                    CultureInfo.GetCultureInfo(_user.Language))!), cancellationToken);
                break;
            }
        }
    }

    public async Task GetStatus(Message message, CancellationToken cancellationToken)
    {
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/get@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    await SendResponse(message, string.Format(_resourceManager.GetString("GetStatus_Chat",
                            CultureInfo.GetCultureInfo(_user.Language))!, $"@{message.From?.Username}"),
                        cancellationToken);
                break;
            case > 0:
            {
                await SendResponse(message, string.Format(_resourceManager.GetString("GetStatus",
                    CultureInfo.GetCultureInfo(_user.Language))!), cancellationToken);
                break;
            }
        }
    }

    public async Task NsfwStatus(Message message, CancellationToken cancellationToken)
    {

        if (_chat is not null || message.Text!.Equals($"/nsfw@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
        {
                await SendResponse(message, string.Format(
                    _resourceManager.GetString("NsfwStatus_Chat",
                        CultureInfo.GetCultureInfo(_user.Language))!,
                    $"@{message.From!.Username}",
                    string.Format(_resourceManager.GetString(_chat?.Nsfw ?? _user.Nsfw ? "OnSwitch" : "OffSwitch",
                            CultureInfo.GetCultureInfo(_user.Language))!),
                    _botClient.GetMeAsync(cancellationToken).Result.Username), cancellationToken); 
        }
        else
        {
            await SendResponse(message, string.Format(
                   _resourceManager.GetString("NsfwStatus",
                       CultureInfo.GetCultureInfo(_user.Language))!,
                    string.Format(_resourceManager.GetString(_user.Nsfw ? "OnSwitch" : "OffSwitch",
                           CultureInfo.GetCultureInfo(_user.Language))!),
                   _botClient.GetMeAsync(cancellationToken).Result.Username), cancellationToken);
        }
    }
    public async Task GetBooruPic(Message message,  CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
        }
        catch (ApiRequestException)
        {
            // ignored
        }

        SearchResult post = default;
        ABooru booru = GetRandomBooru();

        // Could possibly introduce infinite loop. TODO: Implement "Circut Breaker" policy
        if (_chat is not null)
            do
            {
                try
                {
                    post = await booru.GetRandomPostAsync();
                }
                //TODO: Handle different exceptions?
                catch
                {
                    booru = GetRandomBooru();
                }
            } while (!_chat.Nsfw && !post.Rating.Equals(Rating.Safe));
        else
            do
            {
                try
                {
                    post = await booru.GetRandomPostAsync();
                }
                //TODO: Handle different exceptions?
                catch (HttpRequestException)
                {
                    booru = GetRandomBooru();
                }
            } while (!_user.Nsfw && !post.Rating.Equals(Rating.Safe));

        var returnPicMessage = message.Chat.Id > 0
            ? string.Format(_resourceManager.GetString("ReturnPicBooru",
                    CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!,
                post.Rating,
                post.PostUrl)
            : string.Format(_resourceManager.GetString("ReturnPicBooru_Chat",
                    CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!,
                $"@{message.From?.Username}",
                post.Rating,
                post.PostUrl);
        try
        {
            await SendResponse(message, returnPicMessage, cancellationToken, post.FileUrl.AbsoluteUri);
        }
        catch (UnableToParseException)
        {
            await GetBooruPic(message, cancellationToken);
        }
    }


    public async Task GetPicFromReddit(Message message,
        CancellationToken cancellationToken)
    {
        var random = new Random();

        await GetPic(message, random.Next(0, 999), message.Text?[5..]!,
                cancellationToken);
    }

    public async Task GetRandomPic(Message message, CancellationToken cancellationToken)
    {
        var random = new Random();

        if ((message.Chat.Id >= 0 ||
             !message.Text!.Equals(
                 $"/random_reddit@{_botClient.GetMeAsync(cancellationToken).Result.Username}")) &&
            (message.Chat.Id <= 0 || !message.Text!.Equals("/random_reddit"))) return;
        var values = new List<Enum>();

        if (message.Chat.Id < 0)
        {
            if (_chat!.Nsfw)
            {
                values.AddRange(Enum.GetValues(typeof(Explicit)).Cast<Enum>());
                values.AddRange(Enum.GetValues(typeof(Implicit)).Cast<Enum>());
            }
            else
            {
                values.AddRange(Enum.GetValues(typeof(Implicit)).Cast<Enum>());
            }
        }
        else
        {
            if (_user.Nsfw)
            {
                values.AddRange(Enum.GetValues(typeof(Explicit)).Cast<Enum>());
                values.AddRange(Enum.GetValues(typeof(Implicit)).Cast<Enum>());
            }
            else
            {
                values.AddRange(Enum.GetValues(typeof(Implicit)).Cast<Enum>());
            }
        }

        var randomSubreddit = values.ElementAt(random.Next(values.Count));
        //Console.WriteLine(randomSubreddit);

        await GetPic(message, random.Next(0, 999), randomSubreddit.ToString(),
            cancellationToken);
    }

    private async Task GetPic(Message message,
       int randomValue, string subredditString, CancellationToken cancellationToken)
    {
        if (!_configuration.GetSection("REDDIT_APP_ID").Exists())
        {
            await SendResponse(message,
                "Whoops! Something went wrong!!\nReddit credentials isn't present in bot configuration.\nTo use this functionality, please follow the guide listed in README.md file",
                cancellationToken);
            return;
        }

        try
        {
            await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
        }
        catch (ApiRequestException)
        {
            // ignored
        }

        // Console.WriteLine(@"randomValue:" + randomValue);
        // Console.WriteLine(@"subredditString: " + subredditString);

        var lastPostName = "";
        var randomValueMinimized = randomValue;
        var postsCounter = 0;
        var redditClient = new RedditClient(_configuration.GetSection("REDDIT_APP_ID").Value,
            appSecret: _configuration.GetSection("REDDIT_SECRET").Value,
            refreshToken: _configuration.GetSection("REDDIT_REFRESH_TOKEN").Value);

        //In order not to collect the whole collection of [randomValue] posts, minimize this number to specific one in pack of 25.
        while (randomValue > 25)
            randomValue -= 25;
        //Console.WriteLine(@"RandomValue Minimized = " + randomValueMinimized);

        var posts = new List<Post>();
        var subreddit = redditClient.Subreddit(subredditString);
        try
        {
            do
            {
                //If there is no more posts in subreddit
                var testPosts = subreddit.Posts.GetNew(lastPostName, limit: 25);
                if (testPosts.Count == 0)
                    break;

                posts = testPosts;
                postsCounter += posts.Count;
                lastPostName = posts.Last().Fullname;
            } while (postsCounter < randomValue);

            var post = posts.Last();

            //If post is marked as NSFW, try to get the previous one in collection and see, if it tagged as nsfw 
            if (_chat?.Nsfw ?? _user.Nsfw == false)
            {
                var postsInCollection = posts.Count;
                while (post.NSFW)
                {
                    postsInCollection -= 1;
                    post = posts[postsInCollection];
                }
            }

            var returnPicMessage = message.Chat.Id > 0
                ? string.Format(_resourceManager.GetString("ReturnPic",
                        CultureInfo.GetCultureInfo(_user.Language!))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    post.NSFW
                        ? string.Format(_resourceManager.GetString("Yes",
                            CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!)
                        : string.Format(_resourceManager.GetString("No",
                            CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!),
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}")
                : string.Format(_resourceManager.GetString("ReturnPic_Chat",
                        CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!,
                    $"@{message.From?.Username}",
                    $"r/{post.Subreddit}",
                    post.Title,
                    post.NSFW
                        ? string.Format(_resourceManager.GetString("Yes",
                            CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!)
                        : string.Format(_resourceManager.GetString("No",
                            CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!),
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");
            await SendResponse(message, returnPicMessage, cancellationToken, post.Listing.URL);
        }
        //Different reddit exception handlers
        catch (RedditForbiddenException)
        {
            await SendResponse(message, "Whoops! Something went wrong!!\nThis subreddit is banned.",
                cancellationToken);
        }
        catch (RedditNotFoundException)
        {
            await SendResponse(message, "Whoops! Something went wrong!!\nThere is no such subreddit.",
                cancellationToken);
        }
        catch (ApiRequestException)
        {
            await SendResponse(message, "Whoops! Bot is overheated!!!\nPlease, try again later.",
                cancellationToken);
        }
        //If there is no non-NSFW post in collection.
        catch (ArgumentException)
        {
            if (message.Chat.Id < 0)
                await SendResponse(message, string.Format(_resourceManager.GetString("LewdDetected_Chat",
                        CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!, $"@{message.From?.Username}",
                    message.Text), cancellationToken);
            else
                await SendResponse(message, string.Format(_resourceManager.GetString("LewdDetected",
                    CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!, message.Text), cancellationToken);
        }

        //General handler is something goes wrong
        catch (Exception ex)
        {
            await SendResponse(message, $"Whoops! Something went wrong!!!\n{ex.Message}.",
                cancellationToken);
        }
    }

    public async Task SetLanguage(Message message, CancellationToken cancellationToken)
    {
        var language = message.Text?[10..12];

        switch (language)
        {
            case "ru":
                _user.Language = "ru-RU";
                await _context.SaveChangesAsync(cancellationToken);
                break;
            case "en":
                _user.Language = "en-US";
                await _context.SaveChangesAsync(cancellationToken);
                break;
            default:
                await SendResponse(message, "This language is currently not supported", cancellationToken);
                return;
        }

        var setLanguageMessage = string.Format(_resourceManager.GetString("SetLanguage",
            CultureInfo.GetCultureInfo(_user.Language))!);

        _ = SendResponse(message, setLanguageMessage, cancellationToken);
    }


    public async Task TurnNsfw(Message message, CancellationToken cancellationToken)
    {
        string param;
        if (message.Chat.Id < 0 &&
            message.Text!.EndsWith($"@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
            param = message.Text[6..].Split("@")[0];
        else
            param = message.Text![6..];

        if (message.Chat.Id < 0)
        {
            //(bot.GetMeAsync(cancellationToken).Result.Username);
            //Console.WriteLine(param);
            if (message.Text!.EndsWith(_botClient.GetMeAsync(cancellationToken).Result.Username!)) return;
            var admins = await _botClient.GetChatAdministratorsAsync(message.Chat.Id, cancellationToken);
            var isSenderAdmin = admins.Any(member => member.User.Id == message.From!.Id);

            if (isSenderAdmin)
            {
                bool nsfwSetting;
                switch (param)
                {
                    case "on":
                        nsfwSetting = true;
                        break;
                    case "off":
                        nsfwSetting = false;
                        break;
                    default:
                        await NsfwSettingException(message, cancellationToken);
                        return;
                }

                _chat!.Nsfw = nsfwSetting;
                await _context.SaveChangesAsync(cancellationToken);

                await SendResponse(message, string.Format(_resourceManager.GetString(nsfwSetting ? "SetNsfwOn_Chat" : "SetNsfwOff_Chat",
                        CultureInfo.GetCultureInfo(_user.Language))!, $"@{message.From?.Username}"), cancellationToken);
            }
            else
            {
                _ = SendResponse(message, string.Format(_resourceManager.GetString(
                    "NsfwSettingException_NotEnoughRights_Chat",
                    CultureInfo.GetCultureInfo(_user.Language))!), cancellationToken);
            }
        }
        else
        {
            bool nsfwSetting;
            switch (param)
            {
                case "on":
                    nsfwSetting = true;
                    break;
                case "off":
                    nsfwSetting = false;
                    break;
                default:
                    await NsfwSettingException(message, cancellationToken);
                    return;
            }

            _user.Nsfw = nsfwSetting;
            await _context.SaveChangesAsync(cancellationToken);

            _ = SendResponse(message, string.Format(_resourceManager.GetString(nsfwSetting ? 
                "SetNsfwOn" : 
                "SetNsfwOff",
                CultureInfo.GetCultureInfo(_user.Language))!), cancellationToken);
        }
    }

    private async Task NsfwSettingException(Message message,
        CancellationToken cancellationToken)
    {
        if (message.Chat.Id > 0)
            await SendResponse(message, _resourceManager.GetString("NsfwSettingException",
                CultureInfo.GetCultureInfo(_user.Language))!, cancellationToken);
        else
            await SendResponse(message, string.Format(
                _resourceManager.GetString("NsfwSettingException_Chat",
                    CultureInfo.GetCultureInfo(_user.Language))!, $"@{message.From!.Username}",
                $"@{_botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
    }

    public async Task UnknownCommand(Message message, CancellationToken cancellationToken)
    {
/*        if (message.Chat.Id < 0 && message.Text!.StartsWith("/") &&
            message.Text!.EndsWith($"@{_botClient.GetMeAsync(cancellationToken).Result.Username}"))
            await SendResponse(message, string.Format(
                _resourceManager.GetString("UnknownCommand_Chat",
                    CultureInfo.GetCultureInfo(_user.Language))!,
                $"@{message.From?.Username}"), cancellationToken);
        else
        {*/
            await SendResponse(message, _resourceManager.GetString("UnknownCommand",
                 CultureInfo.GetCultureInfo(_user.Language ?? "en-US"))!, cancellationToken);
        //    
    }

    //Method that ensures response delivery
    private async Task SendResponse(Message message,
        string messageText,
        CancellationToken cancellationToken,
        string? picUri = null)
    {
        while (true)
            try
            {
                
                if (picUri != null)
                {
                    await _botClient.SendMediaGroupAsync(message.Chat.Id,
                        new IAlbumInputMedia[] {new InputMediaPhoto(picUri) {Caption = messageText}},
                        cancellationToken: cancellationToken);
                    break;
                }

                await _botClient.SendTextMessageAsync(message.Chat.Id, messageText,
                    cancellationToken: cancellationToken);
                break;

            }
            catch (ApiRequestException exception)
            {

                if (exception.Message.Contains("Bad Request"))
                    throw new UnableToParseException();
                
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception)
            {
                Console.WriteLine("Unhandeled exception faced");
            }
    }

    private ABooru GetRandomBooru()
    {
        var random = new Random();

        var choice = _chat is not null ?
            _chat.Nsfw ? random.Next(6) : random.Next(5) :
            _user.Nsfw ? random.Next(6) : random.Next(5);

        return choice switch
        {
            0 => new Konachan(),
            1 => new SankakuComplex(),
            2 => new DanbooruDonmai(),
            3 => new Lolibooru(),
            4 => new Safebooru(),
            5 => new Sakugabooru(),
            6 => new Yandere(),
            _ => new Konachan()
        };
    }
}