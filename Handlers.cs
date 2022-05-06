using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace owobot_csharp;

public class Handlers
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
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
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

        async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
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
                "/start" => Start(botClient, message),
                "/info" => Info(botClient, message),
                "/status" => Status(botClient, message),
                "/language" => LanguageInfo(botClient, message),
                "/language_ru" => SetLanguageRu(botClient, message),
                _ => UnknownCommand(botClient, message)
            };
            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message)
            {
                await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                // Simulate longer running task
                await Task.Delay(500);

                InlineKeyboardMarkup inlineKeyboard = new(
                    new[]
                    {
                        // first row
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("1.1", "11"),
                            InlineKeyboardButton.WithCallbackData("1.2", "12"),
                        },
                        // second row
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("2.1", "21"),
                            InlineKeyboardButton.WithCallbackData("2.2", "22"),
                        },
                    });

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard);
            }

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

            async Task<Message> Start(ITelegramBotClient botClient, Message message)
            {
                var usage = $"Welcome, {message.From?.FirstName}!\n \n" +
                            "To get more information type /info\n" +
                            "For a quick start, just type /get\n\n" +
                            "Also click on the slash icon at the keyboard to see command list.\n";

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    usage,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }


            async Task<Message> Info(ITelegramBotClient botClient, Message message)
            {
                const string info = $"Hellowo! I'm owobot {Configuration.Version} - a bot that sends cute girls!\n\n" +
                                    "I am written in C# using .NET 6, taking data from reddit, multi-threaded and fully compatible with group chats. Do not be afraid to send me 25-50 requests at a time, I can handle it!\n\n" +
                                    "If you’re tired of reading and you want to see anime girls already, then you are here: /get\n" +
                                    "By default, I will not send you NSFW content, however you can configure this here: /nsfw\n" +
                                    "You can also change the language here: /language\n\n" +
                                    "A few words about privacy - I save the settings for each user and chat, as well as the total number of requests.\n\n" +
                                    "My github page: https://github.com/Rasmus715/owobot-csharp\n\n" +
                                    "I hope that I will be useful to you, master!! (☆ω☆)";

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    info,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
        }

        // Process Inline Keyboard callback data
        async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}");

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

        async Task<Message> LanguageInfo(ITelegramBotClient botClient, Message message)
        {
            var languageInfo = "Oa! Are you decided to change language?\n\n" +
                               "At this moment language is English. Here is what you can do:\n" +
                               "/language_eng to switch to English\n" +
                               "/language_rus чтобы переключится на русский";

            return await botClient.SendTextMessageAsync(message.Chat.Id,
                languageInfo,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> SetLanguageRu(ITelegramBotClient botClient, Message message)
        {
            
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru-RU");
            Console.WriteLine(Thread.CurrentThread.CurrentUICulture.Name);
            var resourceManager = new ResourceManager("Resources.Handler",
                Assembly.GetExecutingAssembly());
            var info = resourceManager.GetString("Info");
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                info,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        async Task<Message> Status(ITelegramBotClient botClient, Message message)
            {
                var x = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

                var status = "I'm alive!\n\n" +
                             $"Uptime: {x.Days} Days " +
                             $"{x:hh\\:mm\\:ss}. " +
                             $"Total requests: {message.MessageId}\n" +
                             "NSFW for this chat: OFF\n" +
                             $"Bot version: {Configuration.Version}";

                return await botClient.SendTextMessageAsync(message.Chat.Id,
                    status,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
        }
    }