using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.EntityFrameworkCore;
using owobot_csharp.Data;
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
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!, applicationContext,resourceManager),
            UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!,applicationContext,resourceManager),
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

        async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message, ApplicationContext applicationContext, ResourceManager resourceManager)
        {
            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;

            var action = message.Text!.Split(' ')[0] switch
            {
                // "/inline" => SendInlineKeyboard(botClient, message),
                // "/keyboard" => SendReplyKeyboard(botClient, message),
                // "/remove" => RemoveKeyboard(botClient, message),
                // "/photo" => SendFile(botClient, message),
                // "/request" => RequestContactAndLocation(botClient, message),
                "/start" => Start(botClient, message, applicationContext),
                "/info" => Info(botClient, message, applicationContext),
                "/status" => Status(botClient, message, applicationContext),
                "/language" => LanguageInfo(botClient, message,applicationContext),
                
                //Unite this two methods by parsind "ru" and "en" value
                "/language_ru" => SetLanguageRu(botClient, message,applicationContext,resourceManager),
                "/language_en" => SetLanguageEn(botClient, message,applicationContext,resourceManager),
                _ => UnknownCommand(botClient, message)
            };
            var sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

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
                    await context.SaveChangesAsync(cancellationToken);
                    
                }

                
                var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
                    Assembly.GetExecutingAssembly());

                var start = string.Format(resourceManager.GetString("Start",CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!, message.From?.FirstName);
                
                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    start,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }


            async Task<Message> Info(ITelegramBotClient botClient, Message message, ApplicationContext context)
            {
                Console.WriteLine(Thread.CurrentThread.CurrentUICulture.Name);
                
                var resourceManager = new ResourceManager("owobot_csharp.Resources.Handlers",
                    Assembly.GetExecutingAssembly());

                var info = string.Format(resourceManager.GetString("Info")!, Configuration.Version);

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
            var languageInfo =  string.Format(resourceManager.GetString("LanguageInfo",CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);
            
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                languageInfo,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> SetLanguageRu(ITelegramBotClient botClient, Message message, ApplicationContext context, ResourceManager resourceManager)
        {

            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            user!.Language = "ru-RU";
            await context.SaveChangesAsync(cancellationToken);

            var setLanguageMessage =  string.Format(resourceManager.GetString("SetLanguage",CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);
            
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                setLanguageMessage,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
        
        async Task<Message> SetLanguageEn(ITelegramBotClient botClient, Message message, ApplicationContext context,ResourceManager resourceManager)
        {
            var user = await context.Users.FirstOrDefaultAsync(c => c.Id == message.From!.Id, cancellationToken);
            user!.Language = "en-US";
            await context.SaveChangesAsync(cancellationToken);

            var setLanguageMessage =  string.Format(resourceManager.GetString("SetLanguage",CultureInfo.GetCultureInfo(user!.Language ?? "en-US"))!);
            
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
                    Configuration.Version);

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    status,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
        }
    }