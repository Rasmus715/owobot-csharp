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
using owobot_csharp.Exceptions;
using owobot_csharp.Interfaces;
using owobot_csharp.Subreddits;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using static System.IO.File;
using Chat = owobot_csharp.Models.Chat;
using User = owobot_csharp.Models.User;

namespace owobot_csharp.Services;

public class HelperService : IHelperService
{
    private readonly ILogger<HelperService> _logger;
    private readonly ApplicationContext _context;

    public HelperService(ILogger<HelperService> logger, ApplicationContext context)
    {
        _logger = logger;
        _context = context;
    }

    private const string TotalRequestsPath = "Essentials/TotalRequests.txt";

    private async Task<User> GetUser(Message message, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(
            c => c.Id == message.From!.Id,
            cancellationToken) ?? await RegisterUser(message, cancellationToken);
    }

    private async Task<User> RegisterUser(Message message, CancellationToken cancellationToken)
    {
        var entity = new User
        {
            Id = message.From!.Id,
            Nsfw = false,
            Language = "en-US"
        };

        await _context.Users.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added user {userId} to DB", message.From!.Id);
        return entity;
    }

    private async Task<Chat?> GetChat(Message message, CancellationToken cancellationToken)
    {
        return message.Chat.Id < 0
            ? await _context.Chats.FirstOrDefaultAsync(c => c.Id == message.Chat.Id,
                cancellationToken) ?? await RegisterChat(message, cancellationToken)
            : null;
    }

    private async Task<Chat> RegisterChat(Message message, CancellationToken cancellationToken)
    {
        var entity = new Chat
        {
            Id = message.Chat.Id,
            Nsfw = false
        };

        await _context.Chats.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added chat {chatId} to DB", message.Chat.Id);
        return entity;
    }

    private static async Task WriteTotalRequests(int requests, CancellationToken cancellationToken)
    {
        requests++;
        await WriteAllTextAsync(TotalRequestsPath, requests.ToString(), cancellationToken);
    }

    private static async Task<int> ReadTotalRequests(CancellationToken cancellationToken)
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

    public async Task SendCustomMessage(Message message, string customMessage, ITelegramBotClient bot,
        CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        _ = SendResponse(message, bot, customMessage, cancellationToken);
    }

    public async Task Start(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/start@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    _ = SendResponse(message, botClient, string.Format(
                            resourceManager.GetString("Start",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                            $"@{message.From?.Username}"),
                        cancellationToken);
                break;
            default:
            {
                _ = SendResponse(message, botClient, string.Format(
                        resourceManager.GetString("Start",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, message.From?.FirstName ?? "User"),
                    cancellationToken);
                break;
            }
        }
    }

    public async Task Info(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var user = await GetUser(message, cancellationToken);

        //Console.WriteLine(message.Text);
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/info@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    _ = SendResponse(message, botClient, string.Format(
                        resourceManager.GetString("Info_Chat",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        $"@{message.From?.Username}", configuration.GetSection("BOT_VERSION").Value,
                        $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
                break;
            case > 0:
            {
                _ = SendResponse(message, botClient, string.Format(
                    resourceManager.GetString("Info",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    configuration.GetSection("BOT_VERSION").Value), cancellationToken);
            }
                break;
        }
    }

    public async Task Status(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);
        var time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        if (message.Chat.Id < 0 &&
            message.Text!.Equals($"/status@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
            _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                time.Days,
                $"{time:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                chat!.Nsfw
                    ? string.Format(resourceManager.GetString("OnSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                    : string.Format(resourceManager.GetString("OffSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                configuration.GetSection("BOT_VERSION").Value), cancellationToken);
        else
            _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                time.Days,
                $"{time:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                user.Nsfw
                    ? string.Format(resourceManager.GetString("OnSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                    : string.Format(resourceManager.GetString("OffSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                configuration.GetSection("BOT_VERSION").Value), cancellationToken);
    }

    public async Task LanguageInfo(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);

        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/language@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("LanguageInfo_Chat",
                            CultureInfo.GetCultureInfo(user.Language))!,
                        $"@{message.From?.Username}",
                        $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
                break;
            case > 0:
            {
                _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("LanguageInfo",
                    CultureInfo.GetCultureInfo(user.Language))!), cancellationToken);
                break;
            }
        }
    }

    public async Task GetStatus(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);

        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/get@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("GetStatus_Chat",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}"),
                        cancellationToken);
                break;
            case > 0:
            {
                _ = SendResponse(message, botClient, string.Format(resourceManager.GetString("GetStatus",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), cancellationToken);
                break;
            }
        }
    }

    public async Task NsfwStatus(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);

        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/nsfw@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                    _ = SendResponse(message, botClient, string.Format(
                        resourceManager.GetString("NsfwStatus_Chat",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        $"@{message.From!.Username}",
                        chat!.Nsfw
                            ? string.Format(resourceManager.GetString("OnSwitch",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                            : string.Format(resourceManager.GetString("OffSwitch",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                        botClient.GetMeAsync(cancellationToken).Result.Username), cancellationToken);
                break;
            case > 0:
            {
                _ = SendResponse(message, botClient, string.Format(
                    resourceManager.GetString("NsfwStatus",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    user.Nsfw
                        ? string.Format(resourceManager.GetString("OnSwitch",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                        : string.Format(resourceManager.GetString("OffSwitch",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    botClient.GetMeAsync(cancellationToken).Result.Username), cancellationToken);

                break;
            }
        }
    }

    public async Task GetRandomBooruPic(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        var chat = await GetChat(message, cancellationToken);
        var user = await GetUser(message, cancellationToken);
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        //Console.WriteLine("got user, chat");

        var random = new Random();

        _ = Task.Run(async () =>
        {
            //Console.WriteLine("Inside the task");
            var choice = message.Chat.Id < 0 ? chat!.Nsfw ? random.Next(6) : random.Next(5) :
                user.Nsfw ? random.Next(6) : random.Next(5);

            switch (choice)
            {
                case 0:
                    await GetBooruPic(new Konachan(), botClient, message, user, chat, cancellationToken);
                    break;
                case 1:
                    await GetBooruPic(new SankakuComplex(), botClient, message, user, chat, cancellationToken);
                    break;
                case 2:
                    await GetBooruPic(new DanbooruDonmai(), botClient, message, user, chat, cancellationToken);
                    break;
                case 3:
                    await GetBooruPic(new Lolibooru(), botClient, message, user, chat, cancellationToken);
                    break;
                case 4:
                    await GetBooruPic(new Safebooru(), botClient, message, user, chat, cancellationToken);
                    break;
                case 5:
                    await GetBooruPic(new Sakugabooru(), botClient, message, user, chat, cancellationToken);
                    break;
                case 6:
                    await GetBooruPic(new Yandere(), botClient, message, user, chat, cancellationToken);
                    break;
                default:
                    await GetBooruPic(new Konachan(), botClient, message, user, chat, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    public async Task GetRandomBooruPic(ITelegramBotClient botClient, InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);

        var random = new Random();

        _ = Task.Run(async () =>
        {

            switch (random.Next(5))
            {
                case 0:
                    await GetBooruPic(new Konachan(), botClient, inlineQuery, cancellationToken);
                    break;
                case 1:
                    await GetBooruPic(new SankakuComplex(), botClient, inlineQuery, cancellationToken);
                    break;
                case 2:
                    await GetBooruPic(new DanbooruDonmai(), botClient, inlineQuery,cancellationToken);
                    break;
                case 3:
                    await GetBooruPic(new Lolibooru(), botClient, inlineQuery, cancellationToken);
                    break;
                case 4:
                    await GetBooruPic(new Safebooru(), botClient, inlineQuery, cancellationToken);
                    break;
                case 5:
                    await GetBooruPic(new Sakugabooru(), botClient, inlineQuery, cancellationToken);
                    break;
                case 6:
                    await GetBooruPic(new Yandere(), botClient, inlineQuery, cancellationToken);
                    break;
                default:
                    await GetBooruPic(new Konachan(), botClient, inlineQuery, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    private async Task GetBooruPic(ABooru booru, ITelegramBotClient botClient, InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        SearchResult post;
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
            do
            {
                post = await booru.GetRandomPostAsync();
            } while (!post.Rating.Equals(Rating.Safe));

        //Console.WriteLine("Got the pic");

        var returnPicMessage = 
            string.Format(resourceManager.GetString("ReturnPicBooru_Chat",
                    CultureInfo.GetCultureInfo("en-US"))!, 
                $"@{inlineQuery.From.Username}", 
                post.Rating,
                post.PostUrl);
        try
        {
            // Console.WriteLine("Sending the pic");
            // Console.WriteLine(post.FileUrl.AbsoluteUri);
            // Console.WriteLine(post.PreviewUrl.AbsoluteUri.Contains("?") ? 
            //     post.PreviewUrl.AbsoluteUri.Substring(0,post.PreviewUrl.AbsoluteUri.IndexOf("?", StringComparison.Ordinal)) 
            //     : post.PreviewUrl.AbsoluteUri);
            _=  SendResponse(inlineQuery, botClient, returnPicMessage, post.FileUrl.AbsoluteUri, 
                post.PreviewUrl.AbsoluteUri.Contains('?') ? 
                    post.PreviewUrl.AbsoluteUri[..post.PreviewUrl.AbsoluteUri.IndexOf('?')] 
                    : post.PreviewUrl.AbsoluteUri,  cancellationToken);
        }
        catch (UnableToParseException)
        {
            await GetBooruPic(booru, botClient, inlineQuery, cancellationToken);
        }
    }

    private static async Task SendResponse(InlineQuery inlineQuery, ITelegramBotClient botClient, string returnPicMessage, string fileUrlAbsoluteUri, string thumbnail, CancellationToken cancellationToken)
    {
        while (true)
            try
            {
                
                var results = new InlineQueryResult[]
                {
                    new InlineQueryResultPhoto("photo:random", fileUrlAbsoluteUri, thumbnail)
                    {
                    Title = "Random pic",
                    Caption = returnPicMessage,
                    Description = "Your result!",
                    PhotoWidth = 158, PhotoHeight = 240,
                        
                    }
                };
                
                await botClient.AnswerInlineQueryAsync(
                    inlineQuery.Id,
                    results,
                    0,
                    cancellationToken: cancellationToken);
                break;
            }
            catch (ApiRequestException exception)
            {
                if (exception.Message.Contains("Bad Request"))
                    throw new UnableToParseException();
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
    }


    private static async Task GetBooruPic(ABooru booru, ITelegramBotClient botClient, Message message, User user,
        Chat? chat, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
        }
        catch (ApiRequestException)
        {
            // ignored
        }


        SearchResult post;
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());


        if (message.Chat.Id < 0)
            do
            {
                post = await booru.GetRandomPostAsync();
            } while (!chat!.Nsfw && !post.Rating.Equals(Rating.Safe));
        else
            do
            {
                post = await booru.GetRandomPostAsync();
            } while (!user.Nsfw && !post.Rating.Equals(Rating.Safe));

        //Console.WriteLine("Got the pic");

        var returnPicMessage = message.Chat.Id > 0
            ? string.Format(resourceManager.GetString("ReturnPicBooru",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                post.Rating,
                post.PostUrl)
            : string.Format(resourceManager.GetString("ReturnPicBooru_Chat",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                $"@{message.From?.Username}",
                post.Rating,
                post.PostUrl);
        try
        {
            // Console.WriteLine("Sending the pic");
            await SendResponse(message, botClient, returnPicMessage, cancellationToken, post.FileUrl.AbsoluteUri);
        }
        catch (UnableToParseException)
        {
            await GetBooruPic(booru, botClient, message, user, chat, cancellationToken);
        }
    }


    public async Task GetPicFromReddit(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);

        //Console.WriteLine(message.Text?[5..]);
        var random = new Random();
        var randomValue = random.Next(0, 999);
        //Console.WriteLine(@"Random value: " + randomValue);

        _ = Task.Run(async () =>
            await GetPic(message, botClient, resourceManager, chat, user, randomValue, message.Text?[5..]!,
                cancellationToken), cancellationToken);
    }

    public async Task GetRandomPic(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var random = new Random();
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);

        if ((message.Chat.Id >= 0 ||
             !message.Text!.Equals(
                 $"/random_reddit@{botClient.GetMeAsync(cancellationToken).Result.Username}")) &&
            (message.Chat.Id <= 0 || !message.Text!.Equals("/random_reddit"))) return;
        var values = new List<Enum>();

        if (message.Chat.Id < 0)
        {
            if (chat!.Nsfw)
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
            if (user.Nsfw)
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

        _ = GetPic(message, botClient, resourceManager, chat, user, random.Next(0, 999), randomSubreddit.ToString(),
            cancellationToken);
    }

    private static async Task GetPic(Message message, ITelegramBotClient botClient, ResourceManager resourceManager,
        Chat? chat, User user, int randomValue, string subredditString, CancellationToken cancellationToken)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        if (!configuration.GetSection("REDDIT_APP_ID").Exists())
        {
            _ = SendResponse(message, botClient,
                "Whoops! Something went wrong!!\nReddit credentials isn't present in bot configuration.\nTo use this functionality, please follow the guide listed in README file",
                cancellationToken);
            return;
        }

        try
        {
            await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
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
        var redditClient = new RedditClient(configuration.GetSection("REDDIT_APP_ID").Value,
            appSecret: configuration.GetSection("REDDIT_SECRET").Value,
            refreshToken: configuration.GetSection("REDDIT_REFRESH_TOKEN").Value);

        //In order not to collect the whole collection of [randomValue] posts, minimize this number to specific one in pack of 25.
        while (randomValueMinimized > 25)
            randomValueMinimized -= 25;
        //Console.WriteLine(@"RandomValue Minimized = " + randomValueMinimized);

        var posts = new List<Post>();
        var subreddit = redditClient.Subreddit(subredditString);
        try
        {
            do
            {
                //If there is no more posts in subreddit
                var testPosts = subreddit.Posts.GetNew(lastPostName, limit: 25);
                if (testPosts.Count == 0) break;

                posts = testPosts;
                postsCounter += posts.Count;
                lastPostName = posts.Last().Fullname;
            } while (postsCounter < randomValue);

            var post = posts.Last();

            //If post is marked as NSFW, try to get the previous one in collection and see, if it tagged as nsfw 
            if (message.Chat.Id < 0)
            {
                if (chat?.Nsfw == false)
                {
                    var postsInCollection = posts.Count;
                    while (post.NSFW)
                    {
                        postsInCollection -= 1;
                        post = posts[postsInCollection];
                    }
                }
            }
            else
            {
                if (user.Nsfw == false)
                {
                    var postsInCollection = posts.Count;
                    while (post.NSFW)
                    {
                        postsInCollection -= 1;
                        post = posts[postsInCollection];
                    }
                }
            }

            var returnPicMessage = message.Chat.Id > 0
                ? string.Format(resourceManager.GetString("ReturnPic",
                        CultureInfo.GetCultureInfo(user.Language!))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    post.NSFW
                        ? string.Format(resourceManager.GetString("Yes",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                        : string.Format(resourceManager.GetString("No",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}")
                : string.Format(resourceManager.GetString("ReturnPic_Chat",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    $"@{message.From?.Username}",
                    $"r/{post.Subreddit}",
                    post.Title,
                    post.NSFW
                        ? string.Format(resourceManager.GetString("Yes",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                        : string.Format(resourceManager.GetString("No",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");

            await SendResponse(message, botClient, returnPicMessage, cancellationToken, post.Listing.URL);
        }
        //Different reddit exception handlers
        catch (RedditForbiddenException)
        {
            await SendResponse(message, botClient, "Whoops! Something went wrong!!\nThis subreddit is banned.",
                cancellationToken);
        }
        catch (RedditNotFoundException)
        {
            await SendResponse(message, botClient, "Whoops! Something went wrong!!\nThere is no such subreddit.",
                cancellationToken);
        }
        catch (ApiRequestException)
        {
            await SendResponse(message, botClient, "Whoops! Bot is overheating!!!\nPlease, try again later.",
                cancellationToken);
        }
        //If there is no non-NSFW post in collection.
        catch (ArgumentException)
        {
            if (message.Chat.Id < 0)
                await SendResponse(message, botClient, string.Format(resourceManager.GetString("LewdDetected_Chat",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}",
                    message.Text), cancellationToken);
            else
                await SendResponse(message, botClient, string.Format(resourceManager.GetString("LewdDetected",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, message.Text), cancellationToken);
        }

        //General handler is something goes wrong
        catch (Exception ex)
        {
            await SendResponse(message, botClient, $"Whoops! Something went wrong!!!\n{ex.Message}.",
                cancellationToken);
        }
    }

    public async Task SetLanguage(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var language = message.Text?[10..12];

        switch (language)
        {
            case "ru":
                user.Language = "ru-RU";
                await _context.SaveChangesAsync(cancellationToken);
                break;
            case "en":
                user.Language = "en-US";
                await _context.SaveChangesAsync(cancellationToken);
                break;
            default:
                _ = SendResponse(message, botClient, "This language is currently not supported", cancellationToken);
                return;
        }

        var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
            CultureInfo.GetCultureInfo(user.Language))!);

        _ = SendResponse(message, botClient, setLanguageMessage, cancellationToken);
    }


    public async Task TurnNsfw(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);

        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        string param;
        if (message.Chat.Id < 0 &&
            message.Text!.EndsWith($"@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
            param = message.Text[6..].Split("@")[0];
        else
            param = message.Text![6..];

        if (message.Chat.Id < 0)
        {
            //(bot.GetMeAsync(cancellationToken).Result.Username);
            //Console.WriteLine(param);
            if (message.Text!.EndsWith(botClient.GetMeAsync(cancellationToken).Result.Username!)) return;
            var admins = await botClient.GetChatAdministratorsAsync(message.Chat.Id, cancellationToken);
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
                        await NsfwSettingException(message, botClient, user, resourceManager, cancellationToken);
                        return;
                }

                chat!.Nsfw = nsfwSetting;
                await _context.SaveChangesAsync(cancellationToken);

                _ = SendResponse(message, botClient, nsfwSetting switch
                {
                    true => string.Format(resourceManager.GetString("SetNsfwOn_Chat",
                        CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From?.Username}"),
                    false => string.Format(resourceManager.GetString("SetNsfwOff_Chat",
                        CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From?.Username}")
                }, cancellationToken);
            }
            else
            {
                _ = SendResponse(message, botClient, string.Format(resourceManager.GetString(
                    "NsfwSettingException_NotEnoughRights_Chat",
                    CultureInfo.GetCultureInfo(user.Language))!), cancellationToken);
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
                    await NsfwSettingException(message, botClient, user, resourceManager, cancellationToken);
                    return;
            }

            user.Nsfw = nsfwSetting;
            await _context.SaveChangesAsync(cancellationToken);

            _ = SendResponse(message, botClient, nsfwSetting switch
            {
                true => string.Format(resourceManager.GetString("SetNsfwOn",
                    CultureInfo.GetCultureInfo(user.Language))!),
                false => string.Format(resourceManager.GetString("SetNsfwOff",
                    CultureInfo.GetCultureInfo(user.Language))!)
            }, cancellationToken);
        }
    }

    private static async Task NsfwSettingException(Message message, ITelegramBotClient botClient, User user,
        ResourceManager resourceManager, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        if (message.Chat.Id > 0)
            _ = SendResponse(message, botClient, resourceManager.GetString("NsfwSettingException",
                CultureInfo.GetCultureInfo(user.Language))!, cancellationToken);
        else
            _ = SendResponse(message, botClient, string.Format(
                resourceManager.GetString("NsfwSettingException_Chat",
                    CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From!.Username}",
                $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken);
    }

    public async Task UnknownCommand(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        if (message.Chat.Id < 0 && message.Text!.StartsWith("/") &&
            message.Text!.EndsWith($"@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
            _ = SendResponse(message, botClient, string.Format(
                resourceManager.GetString("UnknownCommand_Chat",
                    CultureInfo.GetCultureInfo(user.Language))!,
                $"@{message.From?.Username}"), cancellationToken);
    }

    //Method that ensures response delivery
    private static async Task SendResponse(Message message,
        ITelegramBotClient botClient,
        string messageText,
        CancellationToken cancellationToken,
        string? picUri = null)
    {
        while (true)
            try
            {
                if (picUri != null)
                {
                    await botClient.SendMediaGroupAsync(message.Chat.Id,
                        new IAlbumInputMedia[] {new InputMediaPhoto(picUri) {Caption = messageText}},
                        cancellationToken: cancellationToken);
                    break;
                }

                await botClient.SendTextMessageAsync(message.Chat.Id, messageText,
                    cancellationToken: cancellationToken);
                break;
            }
            catch (ApiRequestException exception)
            {
                if (exception.Message.Contains("Bad Request"))
                    //Console.WriteLine("Caught exception. Throwing custom one");
                    throw new UnableToParseException();
                
                await Task.Delay(1000, cancellationToken);
            }
    }
}