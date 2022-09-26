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
    Task GetRandomPic(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken);
    Task GetPicFromReddit(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task SetLanguage(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task TurnNsfw(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);
    Task UnknownCommand(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken);

}

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
        _logger.LogInformation("Successfully added user {userId} to DB", message.From!.Id);
        await _context.SaveChangesAsync(cancellationToken);
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
        _logger.LogInformation("Successfully added chat {chatId} to DB", message.Chat.Id);
        await _context.SaveChangesAsync(cancellationToken);
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
    
    public async Task SendCustomMessage(Message message, string customMessage, ITelegramBotClient bot, CancellationToken cancellationToken)
    {
        await bot.SendTextMessageAsync(message.Chat.Id,
            customMessage,
            cancellationToken: cancellationToken);

        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
    }
    
    public async Task Start(Message message, ITelegramBotClient bot, CancellationToken cancellationToken)
    {
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/start@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                {
                    var name = $"@{message.From?.Username}";
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        string.Format(
                            resourceManager.GetString("Start",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, name),
                        cancellationToken: cancellationToken);
                }

                break;
            case > 0:
            {
                var name = message.From?.FirstName ?? "User";
                await bot.SendTextMessageAsync(message.Chat.Id,
                    string.Format(
                        resourceManager.GetString("Start",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, name),
                    cancellationToken: cancellationToken);
                break;
            }
        }

        await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
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
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(
                            resourceManager.GetString("Info_Chat",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                            $"@{message.From?.Username}", configuration.GetSection("BOT_VERSION").Value,
                            $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"),
                        cancellationToken: cancellationToken);

                    await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                }

                break;
            case > 0:
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(
                        resourceManager.GetString("Info",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        configuration.GetSection("BOT_VERSION").Value),
                    cancellationToken: cancellationToken);

                await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
            }
                break;
        }
    }
    
    public async Task Status(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);

        if (message.Chat.Id < 0 &&
            message.Text!.Equals($"/status@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
        {
            var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var status = string.Format(resourceManager.GetString("Status", 
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                x.Days,
                $"{x:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                chat!.Nsfw
                    ? string.Format(resourceManager.GetString("OnSwitch", 
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                    : string.Format(resourceManager.GetString("OffSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                configuration.GetSection("BOT_VERSION").Value);
            
            await botClient.SendTextMessageAsync(message.Chat.Id,
                status,
                cancellationToken: cancellationToken);
            
            await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        }
        else
        {
            var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var status = string.Format(resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                x.Days,
                $"{x:hh\\:mm\\:ss}",
                await ReadTotalRequests(cancellationToken),
                user.Nsfw
                    ? string.Format(resourceManager.GetString("OnSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                    : string.Format(resourceManager.GetString("OffSwitch",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                configuration.GetSection("BOT_VERSION").Value);
            await botClient.SendTextMessageAsync(message.Chat.Id,
                status, 
                cancellationToken: cancellationToken);
            
            await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        }
    }
    
    public async Task LanguageInfo(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);
        
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/language@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(resourceManager.GetString("LanguageInfo_Chat",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                            $"@{message.From?.Username}",
                            $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"),
                        cancellationToken: cancellationToken);
                    await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                }

                break;
            case > 0:
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(resourceManager.GetString("LanguageInfo",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    cancellationToken: cancellationToken);
                await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                break;
            }
        }
    }
    
    public async Task GetStatus(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);
        
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/get@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(resourceManager.GetString("GetStatus_Chat",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}"),
                        cancellationToken: cancellationToken);

                    await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                }

                break;
            case > 0:
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(resourceManager.GetString("GetStatus",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    cancellationToken: cancellationToken);
                await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                break;
            }
        }
    }
    
    public async Task NsfwStatus(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);
        
        switch (message.Chat.Id)
        {
            case < 0:
                if (message.Text!.Equals($"/nsfw@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
                { 
                    await botClient.SendTextMessageAsync(message.Chat.Id, 
                        string.Format(
                            resourceManager.GetString("NsfwStatus_Chat", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, 
                            $"@{message.From!.Username}", 
                            chat!.Nsfw 
                                ? string.Format(resourceManager.GetString("OnSwitch", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!) 
                                : string.Format(resourceManager.GetString("OffSwitch", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), 
                            botClient.GetMeAsync(cancellationToken).Result.Username), 
                        cancellationToken: cancellationToken);

                    await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                }
                
                break;
            
            case > 0:
            { 
                await botClient.SendTextMessageAsync(message.Chat.Id, 
                    string.Format(
                        resourceManager.GetString("NsfwStatus", 
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, 
                        user.Nsfw 
                            ? string.Format(resourceManager.GetString("OnSwitch", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!) 
                            : string.Format(resourceManager.GetString("OffSwitch", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), 
                        botClient.GetMeAsync(cancellationToken).Result.Username), 
                    cancellationToken: cancellationToken);
                await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
                break; 
            }
        }
    }
    
    public async Task GetRandomBooruPic(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chat = await GetChat(message, cancellationToken);
        var user = await GetUser(message, cancellationToken);
        var random = new Random();

        var choice = message.Chat.Id < 0 ? 
            chat!.Nsfw ? 
                random.Next(6) : 
                random.Next(5) :
            user.Nsfw ? 
                random.Next(6) : 
                random.Next(5);

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
    }
    
    private async Task GetBooruPic(ABooru booru, ITelegramBotClient botClient, Message message, User user, Chat? chat, CancellationToken cancellationToken)
    {
        try
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

            var returnPicMessage = message.Chat.Id > 0
                ? string.Format(resourceManager.GetString("ReturnPicBooru",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    post.Rating,
                    post.FileUrl.AbsoluteUri,
                    post.PostUrl)
                : string.Format(resourceManager.GetString("ReturnPicBooru_Chat",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    $"@{message.From?.Username}",
                    post.Rating,
                    post.FileUrl.AbsoluteUri,
                    post.PostUrl);

            await botClient.SendTextMessageAsync(message.Chat.Id,
                returnPicMessage,
                cancellationToken: cancellationToken);
            
            await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        }
        catch (Exception)
        {
            //ignored
        }
    }
    
    
    public async Task GetPicFromReddit(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);
        //Console.WriteLine(message.Text?[5..]);
        var random = new Random();
        var randomValue = random.Next(0, 999);
        //Console.WriteLine(@"Random value: " + randomValue);

        async void NewThread() => await GetPic(message, botClient, resourceManager, chat, user, randomValue, message.Text?[5..]!, cancellationToken);
        new Thread(NewThread).Start();
    }
    
    public async Task GetRandomPic(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
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
            if (chat!.Nsfw)
            {
                values.AddRange(Enum.GetValues(typeof(Subreddits.Explicit)).Cast<Enum>());
                values.AddRange(Enum.GetValues(typeof(Subreddits.Implicit)).Cast<Enum>());
            }
            else
            {
                values.AddRange(Enum.GetValues(typeof(Subreddits.Implicit)).Cast<Enum>());
            }
        else
        {
            if (user.Nsfw)
            {
                values.AddRange(Enum.GetValues(typeof(Subreddits.Explicit)).Cast<Enum>());
                values.AddRange(Enum.GetValues(typeof(Subreddits.Implicit)).Cast<Enum>());
            }
            else
            {
                values.AddRange(Enum.GetValues(typeof(Subreddits.Implicit)).Cast<Enum>());
            }
        }

        var randomSubreddit = values.ElementAt(random.Next(values.Count));
        //Console.WriteLine(randomSubreddit);

        //Putting this boi into separate thread in order to process multiple requests at once
        async void NewThread() => await GetPic(message, botClient, resourceManager, chat, user, random.Next(0, 999), randomSubreddit.ToString(), cancellationToken);
        new Thread(NewThread).Start();
    }
    
    private static async Task GetPic(Message message, ITelegramBotClient botClient, ResourceManager resourceManager, Chat? chat, User user, int randomValue, string subredditString, CancellationToken cancellationToken)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        
        if (!configuration.GetSection("REDDIT_APP_ID").Exists()) 
        { 
            await botClient.SendTextMessageAsync(message.Chat.Id, 
                "Whoops! Something went wrong!!\nReddit credentials isn't present in bot configuration.\nTo use this functionality, please follow the guide listed in README file", 
                cancellationToken: cancellationToken); 
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
                if (testPosts.Count == 0)
                {
                    break;
                }

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

            await botClient.SendTextMessageAsync(message.Chat.Id,
                returnPicMessage,
                cancellationToken: cancellationToken);
        }
        //Different reddit exception handlers
        catch (RedditForbiddenException)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                "Whoops! Something went wrong!!\nThis subreddit is banned.",
                cancellationToken: cancellationToken);
        }
        catch (RedditNotFoundException)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                "Whoops! Something went wrong!!\nThere is no such subreddit.",
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                "Whoops! Bot is overheating!!!\nPlease, try again later.", 
                cancellationToken: cancellationToken);
        }
        
        //If there is no non-NSFW post in collection.
        catch (ArgumentException)
        {
            Console.WriteLine(@"Lewd detected in Subreddit " + subreddit.Name);
            if (message.Chat.Id < 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(resourceManager.GetString("LewdDetected_Chat", 
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}",
                        message.Text),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(resourceManager.GetString("LewdDetected",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, message.Text),
                    cancellationToken: cancellationToken);
            }
        }

        //General handler is something goes wrong
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                $"Whoops! Something went wrong!!!\n{ex.Message}.", 
                cancellationToken: cancellationToken);
        }
    }
    
    public async Task SetLanguage(ITelegramBotClient botClient, Message message, string language, CancellationToken cancellationToken)
    {
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
        var user = await GetUser(message, cancellationToken);

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
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    "This language is currently not supported",
                    cancellationToken: cancellationToken);
                return;
        }

        var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
            CultureInfo.GetCultureInfo(user.Language))!);

        await botClient.SendTextMessageAsync(message.Chat.Id,
            setLanguageMessage,
            cancellationToken: cancellationToken);
    }
    
    public async Task SetLanguage(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
        var language = message.Text?[10..12];
        
        //Console.WriteLine(language);
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
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    "This language is currently not supported",
                    cancellationToken: cancellationToken);
                return;
        }

        var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
            CultureInfo.GetCultureInfo(user.Language))!);

        await botClient.SendTextMessageAsync(message.Chat.Id,
            setLanguageMessage,
            cancellationToken: cancellationToken);
    }
    
    public async Task TurnNsfw(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        
        var user = await GetUser(message, cancellationToken);
        var chat = await GetChat(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());
        
        string param;
        if (message.Chat.Id < 0 &&
            message.Text!.EndsWith($"@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
            param = message.Text[6..].Split("@")[0];
        else
        {
            param = message.Text![6..];
        }
                
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

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    nsfwSetting switch
                    {
                        true => string.Format(resourceManager.GetString("SetNsfwOn_Chat", 
                            CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From?.Username}"), 
                        false => string.Format(resourceManager.GetString("SetNsfwOff_Chat", 
                            CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From?.Username}")
                    },
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, 
                string.Format(resourceManager.GetString("NsfwSettingException_NotEnoughRights_Chat", 
                    CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From?.Username}"),
                    cancellationToken: cancellationToken);
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
            
            await botClient.SendTextMessageAsync(message.Chat.Id,
                nsfwSetting switch 
                {
                    true => string.Format(resourceManager.GetString("SetNsfwOn", 
                        CultureInfo.GetCultureInfo(user.Language))!),
                    false => string.Format(resourceManager.GetString("SetNsfwOff",
                        CultureInfo.GetCultureInfo(user.Language))!)
                    
                }, cancellationToken: cancellationToken);
        }
    }
    
    private async Task NsfwSettingException(Message message, ITelegramBotClient botClient, User user, ResourceManager resourceManager, CancellationToken cancellationToken)
    {

        if (message.Chat.Id > 0)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                resourceManager.GetString("NsfwSettingException",
                    CultureInfo.GetCultureInfo(user.Language))!,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                string.Format(
                    resourceManager.GetString("NsfwSettingException_Chat",
                        CultureInfo.GetCultureInfo(user.Language))!, $"@{message.From!.Username}",
                    $"@{botClient.GetMeAsync(cancellationToken).Result.Username}"),
                cancellationToken: cancellationToken);
        }
    }

    public async Task UnknownCommand(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var user = await GetUser(message, cancellationToken);
        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        if (message.Chat.Id < 0 && message.Text!.StartsWith("/") &&
            message.Text!.EndsWith($"@{botClient.GetMeAsync(cancellationToken).Result.Username}"))
        {

            await botClient.SendTextMessageAsync(message.Chat.Id,
                string.Format(
                    resourceManager.GetString("UnknownCommand_Chat",
                        CultureInfo.GetCultureInfo(user.Language))!,
                    $"@{message.From?.Username}"),
                cancellationToken: cancellationToken);

            await WriteTotalRequests(await ReadTotalRequests(cancellationToken), cancellationToken);
        }
    }
}