using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.EntityFrameworkCore;
using owobot_csharp.Data;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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



    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var redditClient = new RedditClient(Configuration.Reddit.RedditAppId,
            appSecret: Configuration.Reddit.RedditSecret, refreshToken: Configuration.Reddit.RedditRefreshToken);
        
        var applicationContext = new ApplicationContext();
        Console.WriteLine("applicationContext created");

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
            var user = await applicationContext.Users.FirstOrDefaultAsync(c => message.From != null && c.Id == message.From.Id,
                cancellationToken) ?? await RegisterUser();
            
            Console.WriteLine(Resources.Handlers.Handlers_HandleUpdateAsync_, message.Type, message.From?.Id, message.Text);
            
            if (message.Type != MessageType.Text)
                return;

            switch (message.Text!.ToLower().Split(' ')[0])
            {
                case "owo":
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        "uwu",
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                    break;
                case "uwu":
                    await bot.SendTextMessageAsync(message.Chat.Id,
                        "owo",
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
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
                    if (message.Text.Contains("/get"))
                    {
                        GetPicFromReddit();
                        break;
                    }

                    if (message.Text.Contains("/language"))
                    {
                        await SetLanguage(message.Text[10..]);
                        break;
                    }

                    if (message.Text.Contains("/nsfw"))
                    {
                        await TurnNsfw(message.Text[6..]);
                        Console.WriteLine(message.Text[6..]);
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
                var firstName = message.From?.FirstName ?? "User";
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(
                        resourceManager.GetString("Start",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, firstName),
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }


            async Task Info()
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(
                            resourceManager.GetString("Info",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,
                            Configuration.Telegram.BotVersion),
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            
            async Task LanguageInfo()
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(resourceManager.GetString("LanguageInfo",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            
            async Task UnknownCommand()
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(
                        resourceManager.GetString("UnknownCommand",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,
                        Configuration.Telegram.BotVersion),
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            
            async Task SetLanguage(string language)
            {
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
                            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                        return;
                }

                var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!);

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    setLanguageMessage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }

            async Task Status()
            {
                var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

                var status = string.Format(resourceManager.GetString("Status",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    x.Days,
                    $"{x:hh\\:mm\\:ss}",
                    message.MessageId,
                    user.Nsfw
                        ? "ON"
                        : "OFF",
                    Configuration.Telegram.BotVersion);

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    status,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            
            async Task GetPicNewThread(string? subredditString, int randomValue)
        {
            Console.WriteLine(@"randomValue:" + randomValue);
            Console.WriteLine(@"subredditString: " + subredditString);
            var totalPosts = new List<string>();
            var lastPostName = "";
            randomValue = 999;
            var subreddit = redditClient.Subreddit(subredditString);
            try
            {
                do
                {
                    //await client.SendChatActionAsync(message.From.Id, ChatAction.Typing, cancellationToken);
                    var posts = subreddit.Posts.GetNew(lastPostName, limit: 25).Select(c => c.Permalink);
                    totalPosts.AddRange(posts);
                    lastPostName = posts.Last();
                    Console.WriteLine(@"Total Posts in collection: " + totalPosts.Count);
                } while (totalPosts.Count < randomValue);

                foreach(var x in totalPosts)
                    Console.WriteLine(x);
                
                var post = redditClient.Post($"t3_{totalPosts.Last()}").About();
                
                
                if (user.Nsfw == false)
                {
                    while (post.NSFW)
                    {
                        randomValue -= 1;
                        Console.WriteLine(@"randomValue after decrement:" + randomValue);
                        post = redditClient.Post(totalPosts[randomValue]);
                    }
                }

                var nsfwStatus = user.Language switch
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
                var returnPicMessage = string.Format(resourceManager.GetString("ReturnPic",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    nsfwStatus,
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    returnPicMessage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (RedditForbiddenException)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Something went wrong!!\nThis subreddit is banned.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (RedditNotFoundException)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Something went wrong!!\nThere is no such subreddit.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (ApiRequestException)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Bot is overheating!!!\nPlease, try again.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (InvalidOperationException)
            {
                var random = new Random();
                var post = redditClient.Post(totalPosts.Last());
                var nsfwStatus = user.Language switch
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
                var returnPicMessage = string.Format(resourceManager.GetString("ReturnPic",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    nsfwStatus,
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");

                await botClient.SendTextMessageAsync(message.Chat.Id,
                    returnPicMessage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (ArgumentException)
            {
                var response = string.Format(resourceManager.GetString("LewdDetected",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,message.Text);
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    response,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
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
                Console.WriteLine(@"Successfully added " + message.From?.Id + @" to DB");
                await applicationContext.SaveChangesAsync(cancellationToken);
                return entity;
            }

            void GetPicFromReddit() 
            { 
                Console.WriteLine(message.Text?[5..]);
                var random = new Random();
                var randomValue = random.Next(0, 999);
                Console.WriteLine(@"Random value: " + randomValue);

            async void StartNewThread()
            {
                await GetPicNewThread(message.Text?[5..], randomValue);
            }
            
            new Thread(StartNewThread).Start();
        }

        async Task GetStatus()
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                string.Format(resourceManager.GetString("GetStatus",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty),
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);

        }

        async Task NsfwStatus()
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
            Console.WriteLine(nsfwStatus);
            await botClient.SendTextMessageAsync(message.Chat.Id,
                string.Format(
                    resourceManager.GetString("NsfwStatus",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, nsfwStatus),
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task TurnNsfw(string param)
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
                    true => resourceManager.GetString("SetNsfwOn",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US")),
                    false => resourceManager.GetString("SetNsfwOff",
                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))
                } ?? string.Empty,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            
        }

        async Task GetRandomPic()
        {
            var values = Enum.GetValues(user.Nsfw ? typeof(Subreddits.Explicit) : typeof(Subreddits.Implicit));
            var random = new Random();
            var randomSubreddit = values.GetValue(random.Next(values.Length));

            async void StartNewThread()
            {
                await GetPicNewThread(randomSubreddit?.ToString(), random.Next(0, 999));
            }

            var newThread = new Thread(StartNewThread);

            if (user.Id != null) 
                await botClient.SendChatActionAsync(user.Id, ChatAction.Typing, cancellationToken);
            newThread.Start();
        }

        async Task NsfwSettingException()
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                resourceManager.GetString("NsfwSettingException",
                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
            
        }
        
        Task UnknownUpdateHandlerAsync(Update x)
        {
            //Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}