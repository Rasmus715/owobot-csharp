using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using owobot_csharp.Data;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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

    
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var deserializedJson = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/Configuration.json", cancellationToken));
        
        //TODO: FIX COUNTER!!
        int totalRequests;
        try
        {
            totalRequests =
                int.Parse(await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/TotalRequests.txt",
                    cancellationToken));
        }
        catch (Exception)
        {
            await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/TotalRequests.txt", "0", cancellationToken);
            totalRequests = 0;
        }
        var redditClient = new RedditClient(deserializedJson.RedditAppId,
            appSecret: deserializedJson.RedditSecret, refreshToken: deserializedJson.RedditRefreshToken);
        
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
            Chat chat;
            
            if (message.Chat.Id < 0)
            {
                chat = await applicationContext.Chats.FirstOrDefaultAsync(c => c.Id == message.Chat.Id,
                    cancellationToken) ?? await RegisterChat();
            }

            var user = await applicationContext.Users.FirstOrDefaultAsync(c => message.From != null && c.Id == message.From.Id,
                cancellationToken) ?? await RegisterUser();
            
            //Console.WriteLine(Resources.Handlers.Handlers_HandleUpdateAsync_, message.Type, message.From?.Id, message.Text);
            
            if (message.Type != MessageType.Text)
                return;

            totalRequests++;
            await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/TotalRequests.txt", totalRequests.ToString(), cancellationToken);
            
            switch (message.Text!.ToLower().Split("@")[0])
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
                    GetRandomPic();
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
                        if (message.Text!.Equals("/start@owopics_junior_bot"))
                        { 
                            var name = $"@{message.From?.Username}"; 
                            await botClient.SendTextMessageAsync(message.Chat.Id, 
                            string.Format(
                                resourceManager.GetString("Start", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, name),
                            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        var name = message.From?.FirstName ?? "User";
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                            string.Format(
                                resourceManager.GetString("Start", 
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, name), 
                            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                        break;
                    }
                }
            }


            async Task Info()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals("/info@owopics_junior_bot"))
                        { 
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(
                                    resourceManager.GetString("Info_Chat",
                                        CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, 
                                    $"@{message.From?.Username}", deserializedJson.BotVersion, $"@{bot.GetMeAsync(cancellationToken).Result.Username}"),
                                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(
                                resourceManager.GetString("Info",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty,
                                deserializedJson.BotVersion),
                            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                        break;
                    }
                }
            }
            
            async Task LanguageInfo()
            {
                switch (message.Chat.Id)
                {
                    case < 0:
                        if (message.Text!.Equals("/language@owopics_junior_bot"))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                string.Format(resourceManager.GetString("LanguageInfo_Chat",
                                        CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                                    $"@{message.From?.Username}", $"@{bot.GetMeAsync(cancellationToken).Result.Username}"), cancellationToken: cancellationToken);
                        }
                        break;
                    case > 0:
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager.GetString("LanguageInfo",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!), cancellationToken: cancellationToken);
                        break;
                    }
                }
            }
            
            async Task UnknownCommand()
            {
                if (message.Text!.Contains("/language@owopics_junior_bot"))
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    string.Format(
                        resourceManager.GetString("UnknownCommand",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!),
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
                
                if (message.Chat.Id < 0 && message.Text.Equals("/status@owopics_junior_bot") || message.Chat.Id > 0 && message.Text.Equals("/status"))
                {
                    var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                    var status = string.Format(resourceManager.GetString("Status",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US"))!,
                        x.Days,
                        $"{x:hh\\:mm\\:ss}",
                        totalRequests,
                        user.Nsfw
                            ? "ON"
                            : "OFF",
                        deserializedJson.BotVersion);

                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        status,
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);   
                }
            }
            
            async Task GetPicNewThread(string? subredditString, int randomValue)
            {
            
                await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken);
                // Console.WriteLine(@"randomValue:" + randomValue);
                // Console.WriteLine(@"subredditString: " + subredditString);
                var lastPostName = "";
                var randomValueMinimized = randomValue;
                var postsCounter = 0;
            
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
                 if (user.Nsfw == false)
                 {
                     var postsInCollection = posts.Count;
                     while (post.NSFW)
                     {
                         postsInCollection -= 1;
                         Console.WriteLine(@"randomValueMinimized after decrement:" + postsInCollection);
                         post = posts[postsInCollection];
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
                //Different reddit exceptions handlers
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
                        $"Whoops! Bot is overheating!!!\nPlease, try again later.",
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
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
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(resourceManager.GetString("LewdDetected",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, message.Text),
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                }
                //General handler is something goes wrong
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        $"Whoops! Something went wrong!!!\n{ex.Message}.",
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
                Console.WriteLine(message.Text?[5..]);
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
                    if (message.Text!.Equals("/get@owopics_junior_bot"))
                    { 
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            string.Format(resourceManager.GetString("GetStatus_Chat",
                                CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, $"@{message.From?.Username}"),
                            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                    }
                    break;
                case > 0:
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        string.Format(resourceManager.GetString("GetStatus",
                            CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty),
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
                    break;
                }
            }
        }

        async Task NsfwStatus()
        {
            switch (message.Chat.Id)
            {
                case < 0:
                    if (message.Text!.Equals("/nsfw@owopics_junior_bot"))
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
                                resourceManager.GetString("NsfwStatus_Chat",
                                    CultureInfo.GetCultureInfo(user.Language ?? "en-US")) ?? string.Empty, $"@{message.From!.Username}", nsfwStatus), 
                            cancellationToken: cancellationToken);
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
                    break;
                }
            }
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

        void GetRandomPic()
        {
           
            var values = Enum.GetValues(user.Nsfw ? typeof(Subreddits.Explicit) : typeof(Subreddits.Implicit));
            var random = new Random();
            var randomSubreddit = values.GetValue(random.Next(values.Length));
            
            async void NewThread() =>  await GetPicNewThread(randomSubreddit?.ToString(), random.Next(0, 999));
            new Thread(NewThread).Start();
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