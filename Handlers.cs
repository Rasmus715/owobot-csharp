using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using BooruSharp.Booru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using owobot_csharp.Data;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Chat = owobot_csharp.Models.Chat;
using File = System.IO.File;
using User = owobot_csharp.Models.User;

namespace owobot_csharp;

public static class Handlers
{
    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    private static async Task WriteTotalRequests(string totalRequestsPath, int totalRequests)
    {
        await File.WriteAllTextAsync(totalRequestsPath, totalRequests.ToString());
    }
    
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        const string totalRequestsPath = "Essentials/TotalRequests.txt";
        
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        
        int totalRequests;
        
        try
        {
            totalRequests =
                int.Parse(await File.ReadAllTextAsync(totalRequestsPath, cancellationToken));
        }
        catch (Exception)
        {
            await File.WriteAllTextAsync(totalRequestsPath, "0", cancellationToken);
            totalRequests = 0;
        }

        var applicationContext = new ApplicationContext();

        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }

        async Task BotOnMessageReceived(ITelegramBotClient bot, Message message)
        {
            var chat = message.Chat.Id < 0
                ? await applicationContext.Chats.FirstOrDefaultAsync(c => c.Id == message.Chat.Id,
                    cancellationToken) ?? await RegisterChat()
                : null;

            var user = await applicationContext.Users.FirstOrDefaultAsync(c => message.From != null && c.Id == message.From.Id,
                cancellationToken) ?? await RegisterUser();
            
            //Console.WriteLine(Resources.Handlers.Handlers_HandleUpdateAsync_, message.Type, message.From?.Id, message.Text);
            
            if (message.Type != MessageType.Text)
                return;

            
            switch (message.Text!.ToLower().Split("@")[0])
            {
                case "owo":
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        "uwu", 
                        cancellationToken: cancellationToken);
                    break;
                case "uwu":
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        "owo", 
                        cancellationToken: cancellationToken);
                    break;
                case "/start":
                    await Start();
                    break;
                case "/info":
                    await Info();
                    break;
                case "/status":
                    await Status();
                    break;
                case "/language":
                    await LanguageInfo();
                    break;
                case "/get":
                    await GetStatus();
                    break;
                case "/nsfw":
                    await NsfwStatus();
                    break;
                case "/random":
                    await GetRandomPic();
                    break;
                default:
                    if (message.Text.Contains("/get_"))
                    {
                        GetPicFromReddit();
                        break;
                    }

                    if (message.Text.Contains("/language"))
                    {
                        await SetLanguage(message.Text[10..12]);
                        break;
                    }

                    if (message.Text.Contains("/nsfw"))
                    {
                        if (message.Chat.Id < 0 && message.Text.EndsWith($"@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                            await TurnNsfw(message.Text[6..].Split("@")[0]);
                        if (message.Chat.Id > 0)
                            await TurnNsfw(message.Text[6..]);
                        break;
                    }
                    else
                    {
                        await UnknownCommand();
                        break;
                    }
            }


            async Task Start()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals($"/start@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                        { 
                            var name = $"@{message.From?.Username}"; 
                            await botClient.SendTextMessageAsync(message.Chat.Id, 
                            string.Format(
                                resourceManager.GetString("Start", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, name), 
                            cancellationToken: cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        var name = message.From?.FirstName ?? "User";
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                            string.Format(
                                resourceManager.GetString("Start", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, name), 
                            cancellationToken: cancellationToken);
                        break;
                    }
                }
                
                totalRequests++;
                await File.WriteAllTextAsync(totalRequestsPath, totalRequests.ToString(), cancellationToken);
            }


            async Task Info()
            {
                Console.WriteLine(message.Text);
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals($"/info@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                        { 
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(
                                    resourceManager.GetString("Info_Chat",
                                        CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, 
                                    $"@{message.From?.Username}", configuration.GetSection("BOT_VERSION").Value, $"@{bot.GetMeAsync(cancellationToken).Result.Username}"),
                            cancellationToken: cancellationToken);
                            
                            totalRequests++;
                            await File.WriteAllTextAsync($"{Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName}/TotalRequests.txt", totalRequests.ToString(), cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(
                                resourceManager.GetString("Info",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,
                                configuration.GetSection("BOT_VERSION").Value), 
                            cancellationToken: cancellationToken);
                        
                        totalRequests++;
                        await File.WriteAllTextAsync(totalRequestsPath, totalRequests.ToString(), cancellationToken);
                        break;
                    }
                }
                
            }
            
            async Task LanguageInfo()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals($"/language@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(resourceManager.GetString("LanguageInfo_Chat",
                                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                                    $"@{message.From?.Username}", $"@{bot.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken: cancellationToken);
                            totalRequests++;
                            await File.WriteAllTextAsync($"{Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName}/TotalRequests.txt", totalRequests.ToString(), cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager.GetString("LanguageInfo",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), cancellationToken: cancellationToken);
                        totalRequests++;
                        await File.WriteAllTextAsync($"{Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName}/TotalRequests.txt", totalRequests.ToString(), cancellationToken);
                        break;
                    }
                }
            }
            
            async Task UnknownCommand()
            {
                if (message.Text!.Contains($"/language@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                {
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(
                            resourceManager.GetString("UnknownCommand",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                        cancellationToken: cancellationToken);
                    
                    totalRequests++;
                    await File.WriteAllTextAsync($"{Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName}/TotalRequests.txt", totalRequests.ToString(), cancellationToken);
                }
                
            }
            
            async Task SetLanguage(string language)
            {
                Console.WriteLine(language);
                switch (language)
                {
                    case "ru":
                        user.Language = "ru-RU";
                        await applicationContext.SaveChangesAsync(cancellationToken);
                        break;
                    case "en":
                        user.Language = "en-US";
                        await applicationContext.SaveChangesAsync(cancellationToken);
                        break;
                    default:
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "This language is currently not supported", 
                            cancellationToken: cancellationToken);
                        return;
                }

                var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!);

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    setLanguageMessage, 
                    cancellationToken: cancellationToken);
            }

            async Task Status()
            {
                if (message.Chat.Id < 0 && message.Text.Equals($"/status@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                {
                    totalRequests++;
                    await File.WriteAllTextAsync(totalRequestsPath, totalRequests.ToString(), cancellationToken);

                    var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                    var status = string.Format(resourceManager.GetString("Status",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        x.Days,
                        $"{x:hh\\:mm\\:ss}",
                        totalRequests,
                        user.Language == "ru-RU" ? 
                            chat!.Nsfw ? 
                                "Включён" : 
                                "Выключен" : 
                            chat!.Nsfw ? 
                                "ON" : 
                                "OFF",
                        configuration.GetSection("BOT_VERSION").Value);

                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        status, 
                        cancellationToken: cancellationToken);
                }
                
                else
                {
                    totalRequests++;
                    await File.WriteAllTextAsync(totalRequestsPath, totalRequests.ToString(), cancellationToken);

                    var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                    var status = string.Format(resourceManager.GetString("Status",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        x.Days,
                        $"{x:hh\\:mm\\:ss}",
                        totalRequests,
                        user.Language!.Equals("ru_RU") 
                            ? user.Nsfw 
                                ? @"Включён" 
                                : @"Выключен"
                        : user.Nsfw
                        ? "ON"
                        : "OFF",
                        configuration.GetSection("BOT_VERSION").Value);

                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        status, 
                        cancellationToken: cancellationToken);
                }
            }
            
            async Task GetPicNewThread(string? subredditString, int randomValue)
            {
                try
                {
                    await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
                }
                catch (ApiRequestException)
                {
                    // ignored
                }

                ;
                // Console.WriteLine(@"randomValue:" + randomValue);
                // Console.WriteLine(@"subredditString: " + subredditString);
                var lastPostName = "";
                var randomValueMinimized = randomValue;
                var postsCounter = 0;
                var redditClient = new RedditClient(configuration.GetSection("REDDIT_APP_ID").Value,
                    appSecret: configuration.GetSection("REDDIT_SECRET").Value, refreshToken: configuration.GetSection("REDDIT_REFRESH_TOKEN").Value);

                //In order not to collect the whole collection of [randomValue] posts, minimize this number to specific one in pack of 25.
                while (randomValueMinimized > 25)
                    randomValueMinimized -= 25; 
                //Console.WriteLine(@"RandomValue Minimized = " + randomValueMinimized);

                var posts = new List<Post>();
                var subreddit = redditClient!.Subreddit(subredditString);
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
                    } 
                    while (postsCounter < randomValue);
                    
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
                                //Console.WriteLine(@"randomValueMinimized after decrement:" + postsInCollection);
                                post = posts[postsInCollection];
                            }
                        }
                    }
                    else
                    {
                        if (user?.Nsfw == false)
                        {
                            var postsInCollection = posts.Count;
                            while (post.NSFW)
                            {
                                postsInCollection -= 1;
                                //Console.WriteLine(@"randomValueMinimized after decrement:" + postsInCollection);
                                post = posts[postsInCollection];
                            }
                        }
                    }
                 

                    var nsfwStatus = user?.Language switch
                    {
                        "en-US" => post.NSFW switch
                        {
                            true => "Yes",
                            _ => "No"
                        },
                        "ru-RU" => post.NSFW switch
                        {
                            true => "Да",
                            _ => "Нет"
                        },
                        _ => "No"
                    };

                    var returnPicMessage = message.Chat.Id > 0 ?
                        string.Format(resourceManager!.GetString("ReturnPic", 
                                CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!, 
                            $"r/{post.Subreddit}",
                            post.Title,
                            nsfwStatus,
                            post.Listing.URL,
                            $"https://reddit.com{post.Permalink}") :
                        string.Format(resourceManager!.GetString("ReturnPic_Chat", 
                                CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!,
                            $"@{message.From?.Username}",
                            $"r/{post.Subreddit}",
                            post.Title,
                            nsfwStatus,
                            post.Listing.URL,
                            $"https://reddit.com{post.Permalink}");
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, 
                        returnPicMessage, 
                        cancellationToken: cancellationToken);
                    
                    GC.Collect();
                }
                
                //Different reddit exceptions handlers
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
                
                //Previous exception handler, if there was no posts in subreddit. Don't need it anymore
                // catch (InvalidOperationException)
                // {
                //     var random = new Random();
                //     Console.WriteLine("Post.Count in exception: " + posts.Count);
                //     var post = posts[random.Next(1, randomValueMinimized)];
                //     var nsfwStatus = user.Language switch
                //     {
                //         "en-US" => post.NSFW switch
                //         {
                //             true => "Yes",
                //             _ => "No"
                //         },
                //         "ru-RU" => post.NSFW switch
                //         {
                //             true => "Да",
                //             _ => "Нет"
                //         },
                //         _ => "No"
                //     };
                //     var returnPicMessage = string.Format(resourceManager.GetString("ReturnPic",
                //             CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                //         $"r/{post.Subreddit}",
                //         post.Title,
                //         nsfwStatus,
                //         post.Listing.URL,
                //         $"https://reddit.com{post.Permalink}");
                //
                //     return await botClient.SendTextMessageAsync(message.Chat.Id,
                //         returnPicMessage,
                //         replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                // }
                
                //If there is no non-NSFW post in collection.
                catch (ArgumentException)
                {
                    if (message.Chat.Id < 0)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager!.GetString("LewdDetected_Chat",
                                CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!, $"@{message.From?.Username}", message.Text), 
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager!.GetString("LewdDetected",
                                CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!, message.Text), 
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

            async Task<User> RegisterUser()
            {
                var entity = new User
                    {
                        Id = message.From?.Id,
                        Nsfw = false,
                        Language = "en-US"
                    };


                    await applicationContext.Users.AddAsync(entity, cancellationToken);
                    Console.WriteLine(@"Successfully added user " + message.From?.Id + @" to DB");
                    await applicationContext.SaveChangesAsync(cancellationToken);
                    return entity;
            }

            async Task<Chat> RegisterChat()
            {
                var entity = new Chat
                {
                    Id = message.Chat.Id,
                    Nsfw = false
                };
                
                await applicationContext.Chats.AddAsync(entity, cancellationToken);
                Console.WriteLine(@"Successfully added chat " + message.Chat.Id + @" to DB");
                await applicationContext.SaveChangesAsync(cancellationToken);
                return entity;
            }
            
            

            void GetPicFromReddit() 
            { 
                
                //Console.WriteLine(message.Text?[5..]);
                var random = new Random();
                var randomValue = random.Next(0, 999);
                //Console.WriteLine(@"Random value: " + randomValue);

                async void NewThread() => await GetPicNewThread(message.Text?[5..], randomValue);
                new Thread(NewThread).Start();

            }

            async Task GetStatus()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals($"/get@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                        { 
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(resourceManager.GetString("GetStatus_Chat",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, $"@{message.From?.Username}"), 
                                cancellationToken: cancellationToken);
                        
                            totalRequests++;
                            await WriteTotalRequests(totalRequestsPath, totalRequests);
                        }
                        
                        break;
                    case > 0:
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager.GetString("GetStatus",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty), 
                            cancellationToken: cancellationToken);
                    
                        totalRequests++;
                        await WriteTotalRequests(totalRequestsPath, totalRequests);
                        
                        break;
                    } 
                }
            }

            async Task NsfwStatus()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals($"/nsfw@{bot.GetMeAsync(cancellationToken).Result.Username}"))
                        { 
                            var nsfwStatus = user.Language switch
                            {
                                "en-US" => chat!.Nsfw switch
                                {
                                    true => "Enabled",
                                    _ => "Disabled"
                                },
                                "ru-RU" => chat!.Nsfw switch
                                {
                                    true => "Включен",
                                    _ => "Выключен"
                                },
                                _ => "Unknown. Please, type /start"
                            };
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(
                                    resourceManager.GetString("NsfwStatus_Chat",
                                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From!.Username}", 
                                    nsfwStatus, 
                                    bot.GetMeAsync( cancellationToken).Result.Username), 
                                cancellationToken: cancellationToken);
                        
                            totalRequests++;
                            await WriteTotalRequests(totalRequestsPath, totalRequests);
                        } 
                        break;
                    
                    case > 0: 
                    { 
                        var nsfwStatus = user.Language switch 
                        {
                            "en-US" => user.Nsfw switch
                        {
                            true => "Enabled",
                            _ => "Disabled"
                        },
                        "ru-RU" => user.Nsfw switch
                        {
                            true => "Включен",
                            _ => "Выключен"
                        },
                        _ => "Unknown. Please, type /start"
                    };
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                            string.Format(
                                resourceManager.GetString("NsfwStatus", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, nsfwStatus), 
                            cancellationToken: cancellationToken);
                        
                        totalRequests++;
                        await WriteTotalRequests(totalRequestsPath, totalRequests);
                        break; 
                    } 
                } 
            }
            
            async Task TurnNsfw(string param) 
            { 
                if (message.Chat.Id < 0) 
                { 
                    //(bot.GetMeAsync(cancellationToken).Result.Username);
                    //Console.WriteLine(param);
                    if (!message.Text.EndsWith(bot.GetMeAsync(cancellationToken).Result.Username!))
                        return; 
                    var admins = await bot.GetChatAdministratorsAsync(message.Chat.Id, cancellationToken: cancellationToken); 
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
                                await NsfwSettingException(); 
                                return; 
                        }
                        
                        chat!.Nsfw = nsfwSetting; 
                        await applicationContext.SaveChangesAsync(cancellationToken);
                        
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                            nsfwSetting switch 
                            {
                                true => string.Format(resourceManager.GetString("SetNsfwOn_Chat", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}"),
                                false => string.Format(resourceManager.GetString("SetNsfwOff_Chat", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}")
                            }, 
                            cancellationToken: cancellationToken);
                        
                        totalRequests++;
                        await WriteTotalRequests(totalRequestsPath, totalRequests);
                        
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, string.Format(resourceManager.GetString(
                            "NsfwSettingException_NotEnoughRights_Chat",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, $"@{message.From?.Username}"), cancellationToken: cancellationToken);
                        totalRequests++;
                        await WriteTotalRequests(totalRequestsPath, totalRequests);
                        
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
                            await NsfwSettingException(); 
                            return; 
                    }
                    
                    user.Nsfw = nsfwSetting; 
                    await applicationContext.SaveChangesAsync(cancellationToken);
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, 
                        nsfwSetting switch 
                        {
                            true => string.Format(resourceManager.GetString("SetNsfwOn", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), 
                            false => string.Format(resourceManager.GetString("SetNsfwOff", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!)
                        }, 
                        cancellationToken: cancellationToken);
                    totalRequests++;
                    await WriteTotalRequests(totalRequestsPath, totalRequests);
                    
                } 
            }
            
            async Task GetRandomPic() 
            {
                if (message.Chat.Id < 0 && message.Text.Equals($"/random@{bot.GetMeAsync(cancellationToken).Result.Username}") || 
                    message.Chat.Id > 0 && message.Text.Equals("/random")) 
                {
                    totalRequests++;
                    await WriteTotalRequests(totalRequestsPath, totalRequests);
                    
                    var values = message.Chat.Id < 0 ? 
                        Enum.GetValues(chat!.Nsfw ? typeof(Subreddits.Explicit) : typeof(Subreddits.Implicit)) : 
                        Enum.GetValues(user.Nsfw ? typeof(Subreddits.Explicit) : typeof(Subreddits.Implicit));
                    
                    var random = new Random(); 
                    var randomSubreddit = values.GetValue(random.Next(values.Length));
                    
                    //Putting this boi in separate thread in order to process multiple requests at one time
                    async void NewThread() =>  await GetPicNewThread(randomSubreddit?.ToString(), random.Next(0, 999));
                    new Thread(NewThread).Start(); 
                } 
            }
            
            async Task NsfwSettingException() 
            { 
                if (message.Chat.Id < 0) 
                { 
                    await botClient.SendTextMessageAsync(message.Chat.Id, 
                        resourceManager.GetString("NsfwSettingException",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, 
                        cancellationToken: cancellationToken); 
                }
                else 
                { 
                    await botClient.SendTextMessageAsync(message.Chat.Id, 
                        string.Format(
                            resourceManager.GetString("NsfwSettingException_Chat", 
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!, message.From!.Id, $"@{bot.GetMeAsync(cancellationToken).Result.Username}"), 
                        cancellationToken: cancellationToken); 
                } 
            } 
        }
        
        Task UnknownUpdateHandlerAsync(Update x)
        {
            //Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
        
    }
    
    
}