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
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
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

        var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
            Assembly.GetExecutingAssembly());

        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!, applicationContext, resourceManager),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
            UpdateType.InlineQuery => BotOnInlineQueryReceived(botClient, update.InlineQuery!),
            UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }

        async Task BotOnMessageReceived(ITelegramBotClient bot, Message message,
            ApplicationContext appDbContext, ResourceManager resources)
        {
            Console.WriteLine(
                $"Receive message type: {message.Type} \nFrom: {message.From?.Id} \nBody: {message.Text}");
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
                    Start(bot, message, appDbContext);
                    break;
                case "/info":
                    Info(bot, message, appDbContext);
                    break;
                case "/status":
                    Status(bot, message, appDbContext);
                    break;
                case "/language":
                    LanguageInfo(bot, message, appDbContext);
                    break;
                case "/get":
                    GetStatus(message, appDbContext);
                    break;
                case "/nsfw":
                    NsfwStatus(message, appDbContext);
                    break;
                case "/random":
                    GetRandomPic(message, appDbContext);
                    break;
                default:
                    if (message.Text.Contains("/get"))
                    {
                        GetPicFromReddit(message, appDbContext);
                        break;
                    }

                    if (message.Text.Contains("/language"))
                    {
                        var language = message.Text[10..];
                        SetLanguage(bot, message, appDbContext, resources, language);
                        break;
                    }

                    if (message.Text.Contains("/nsfw"))
                    {
                        TurnNsfw(message, appDbContext, message.Text[6..]);
                        Console.WriteLine(message.Text[6..]);
                        break;
                    }
                    else
                    {
                        UnknownCommand(bot, message);
                        break;
                    }
            }

            // var action = message.Text!.ToLower().Split(' ')[0] switch
            // {
            //     // "/inline" => SendInlineKeyboard(botClient, message),
            //     // "/keyboard" => SendReplyKeyboard(botClient, message),
            //     // "/remove" => RemoveKeyboard(botClient, message),
            //     // "/photo" => SendFile(botClient, message),
            //     // "/request" => RequestContactAndLocation(botClient, message),
            //     "owo" => Owo(message), //Too bad can't return SendTextMessageAsync here :(
            //     "uwu" => Uwu(message),
            //     "/start" => Start(botClient, message, applicationContext),
            //     "/info" => Info(botClient, message, applicationContext),
            //     "/status" => Status(botClient, message, applicationContext),
            //     "/language" => LanguageInfo(botClient, message,applicationContext),
            //     "/get"=> GetStatus(message,applicationContext,redditClient),
            //     
            //     //Unite this two methods by parsind "ru" and "en" value
            //     "/language_ru" => SetLanguageRu(botClient, message,applicationContext,resourceManager),
            //     "/language_en" => SetLanguageEn(botClient, message,applicationContext,resourceManager),
            //     
            // };


            static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message)
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new(
                    new[]
                    {
                        new KeyboardButton[] {"1.1", "1.2"},
                        new KeyboardButton[] {"2.1", "2.2"},
                    })
                {
                    ResizeKeyboard = true
                };

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup);
            }

            static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message)
            {
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Removing keyboard",
                    replyMarkup: new ReplyKeyboardRemove());
            }

            static async Task<Message> SendFile(ITelegramBotClient botClient, Message message)
            {
                await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string filePath = @"Files/tux.png";
                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

                return await botClient.SendPhotoAsync(chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture");
            }

            static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message)
            {
                ReplyKeyboardMarkup RequestReplyKeyboard = new(
                    new[]
                    {
                        KeyboardButton.WithRequestLocation("Location"),
                        KeyboardButton.WithRequestContact("Contact"),
                    });

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: RequestReplyKeyboard);
            }

            async Task<Message> Start(ITelegramBotClient botClient, Message message, ApplicationContext context)
            {

                var user = await context.Users.FirstOrDefaultAsync(c => message.From != null && c.Id == message.From.Id,
                    cancellationToken);

                if (user == null)
                {
                    var entity = new User
                    {
                        Id = message.From?.Id,
                        Nsfw = false,
                        Language = "en-US"
                    };

                    
                    await context.Users.AddAsync(entity, cancellationToken);
                    Console.WriteLine("Succesfully added " + message.From.Id + " to DB");
                    await context.SaveChangesAsync(cancellationToken);

                }


                var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
                    Assembly.GetExecutingAssembly());

                var start = string.Format(
                    resourceManager.GetString("Start",
                        CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language)), message.From?.FirstName);

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    start,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }


            async Task<Message> Info(ITelegramBotClient botClient, Message message, ApplicationContext context)
            {
                var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From.Id);

                var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
                    Assembly.GetExecutingAssembly());

                var info = string.Format(
                    resourceManager.GetString("Info",
                        CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language)),
                    Configuration.Telegram.BotVersion);

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    info,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
        }

        // Process Inline Keyboard callback data
        async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"Received {callbackQuery.Data}");

            if (callbackQuery.Message != null)
                await botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"Received {callbackQuery.Data}");
        }

        async Task BotOnInlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

            InlineQueryResult[] results =
            {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };

            await botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0);
        }

        Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient,
            ChosenInlineResult chosenInlineResult)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }

        async Task<Message> UnknownCommand(ITelegramBotClient botClient, Message message)
        {
            const string unknownCommand = "Eaah? I don't understand you!";
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                unknownCommand,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> LanguageInfo(ITelegramBotClient botClient, Message message, ApplicationContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            var languageInfo = string.Format(resourceManager.GetString("LanguageInfo",
                CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);

            return await botClient.SendTextMessageAsync(message.Chat.Id,
                languageInfo,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> SetLanguage(ITelegramBotClient botClient, Message message, ApplicationContext context,
            ResourceManager resourceManager, string language)
        {

            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            switch (language)
            {
                case "ru":
                    user!.Language = "ru-RU";
                    await context.SaveChangesAsync(cancellationToken);
                    break;
                case "en":
                    user!.Language = "en-US";
                    await context.SaveChangesAsync(cancellationToken);
                    break;
                default:
                    return await botClient.SendTextMessageAsync(message.Chat.Id,
                        "This language is currently not supported",
                        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }

            var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
                CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);

            return await botClient.SendTextMessageAsync(message.Chat.Id,
                setLanguageMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> SetLanguageEn(ITelegramBotClient botClient, Message message, ApplicationContext context,
            ResourceManager resourceManager)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            user!.Language = "en-US";
            await context.SaveChangesAsync(cancellationToken);

            var setLanguageMessage = string.Format(resourceManager.GetString("SetLanguage",
                CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);

            return await botClient.SendTextMessageAsync(message.Chat.Id,
                setLanguageMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> Status(ITelegramBotClient botClient, Message message, ApplicationContext context)
        {
            var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);

            var status = string.Format(resourceManager.GetString("Status",
                    CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!,
                x.Days,
                $"{x:hh\\:mm\\:ss}",
                message.MessageId,
                user.Nsfw
                    ? "ON"
                    : "OFF",
                Configuration.Telegram.BotVersion);

            return await botClient.SendTextMessageAsync(message.Chat.Id,
                status,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }


        
        async Task<Message> GetPicNewThread(Message message, string subredditString, int randomValue, User user)
        {
            List<Post> posts;
            Console.WriteLine("randomValue:" + randomValue);
            Console.WriteLine("subredditString: " + subredditString);
            var totalPosts = new List<Post>();
            var lastPostName = "";
            try
            {
                do
                {
                    var subreddit = redditClient.Subreddit(subredditString);
                    //await botClient.SendChatActionAsync(message.From.Id, ChatAction.Typing, cancellationToken);
                    posts = subreddit.Posts.GetNew(lastPostName, limit: 25);
                    totalPosts.AddRange(posts);
                    lastPostName = posts.Last().Fullname;
                    Thread.Sleep(5);
                    Console.WriteLine("Total Posts in collection: " + totalPosts.Count);
                } while (totalPosts.Count < randomValue);

                var post = totalPosts[randomValue];
                
                
                if (user.Nsfw == false)
                {
                    while (post.NSFW)
                    {
                        randomValue -= 1;
                        Console.WriteLine("randomValue after decrement:" + randomValue);
                        post = totalPosts[randomValue];
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
                        CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    nsfwStatus,
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    returnPicMessage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (RedditForbiddenException)
            {
                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Something went wrong!!\nThis subreddit is banned.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (RedditNotFoundException)
            {
                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Something went wrong!!\nThere is no such subreddit.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (ApiRequestException)
            {
                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Whoops! Bot is overheating!!!\nPlease, try again.",
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (InvalidOperationException)
            {
                var random = new Random();
                var post = totalPosts[random.Next(totalPosts.Count)];
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
                        CultureInfo.GetCultureInfo(user?.Language ?? "en-US"))!,
                    $"r/{post.Subreddit}",
                    post.Title,
                    nsfwStatus,
                    post.Listing.URL,
                    $"https://reddit.com{post.Permalink}");

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    returnPicMessage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (ArgumentException)
            {
                var response = string.Format(resourceManager.GetString("LewdDetected",
                    CultureInfo.GetCultureInfo(user?.Language ?? "en-US")),message.Text);
                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    response,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }

        }

        async Task RegisterUser(ApplicationContext context,Message message)
        {
            var entity = new User
            {
                Id = message.From?.Id,
                Nsfw = false,
                Language = "en-US"
            };

                    
            await context.Users.AddAsync(entity, cancellationToken);
            Console.WriteLine("Succesfully added " + message.From.Id + " to DB");
            await context.SaveChangesAsync(cancellationToken);
        }
        async Task GetPicFromReddit(Message message, ApplicationContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            if (user == null)
                RegisterUser(context, message);
            Console.WriteLine(message.Text?[5..]);
            var random = new Random();
            var randomValue = random.Next(0, 999);
            Console.WriteLine("randomvalue: " + randomValue);

            var newThread = new Thread(async () => { await GetPicNewThread(message, message.Text?[5..], randomValue, user); });
            newThread.Start();
        }

        async Task<Message> GetStatus(Message message, ApplicationContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            var getStatus = string.Format(resourceManager.GetString("GetStatus",
                CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language)));
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                getStatus,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);

        }

        async Task NsfwStatus(Message message, ApplicationContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
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
            var nsfwStatusMessage =
                string.Format(
                    resourceManager.GetString("NsfwStatus",
                        CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language)), nsfwStatus);
            Console.WriteLine(nsfwStatus);
            await botClient.SendTextMessageAsync(message.Chat.Id,
                nsfwStatusMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> TurnNsfw(Message message, ApplicationContext context, string param)
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
                    return await NsfwException(message,context,resourceManager);
            }
           

            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            if (user != null)
            {
                user.Nsfw = nsfwSetting;
                await context.SaveChangesAsync(cancellationToken);
            }

            var responseMessage = nsfwSetting switch
            {
                true => resourceManager.GetString("SetNsfwOn",
                    CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language)),
                false => resourceManager.GetString("SetNsfwOff",
                    CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language))
            };
            
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                responseMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            
        }

        async Task GetRandomPic(Message message, ApplicationContext context)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            var values = Enum.GetValues(user.Nsfw ? typeof(Subreddits.Explicit) : typeof(Subreddits.Implicit));
            var random = new Random();
            var randomSubreddit = values.GetValue(random.Next(values.Length));
            var newThread = new Thread(async () => { await GetPicNewThread(message, randomSubreddit.ToString(), random.Next(0, 999), user); });
            await botClient.SendChatActionAsync(message.From.Id, ChatAction.Typing, cancellationToken);
            newThread.Start();
        }

        async Task<Message> NsfwException(Message message, ApplicationContext context,ResourceManager resourceManager)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            var responseMessage = resourceManager.GetString("NsfwException",
                CultureInfo.GetCultureInfo(user == null ? "en-US" : user.Language));
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                responseMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
    }
}