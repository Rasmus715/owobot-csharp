using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using owobot_csharp;
using owobot_csharp.Abstract;
using owobot_csharp.Data;
using owobot_csharp.Extensions;
using owobot_csharp.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("Program");

IConfiguration configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var validator = new Validator(configuration);
try
{
    validator.Validate();
}
catch (ValidationException)
{
    logger.LogError("Please, fix the errors listed above and try again");
    return 1;
}

if (!Directory.Exists("Essentials"))
{
    logger.LogInformation(@"Creating ""Essentials"" Directory");
    Directory.CreateDirectory("Essentials");
}

try
{
    await using var applicationContext = new ApplicationContext();

    if (applicationContext.Database.GetAppliedMigrations().LastOrDefault() is null)
    {
        logger.LogInformation("Initializing migration...");
        applicationContext.Database.Migrate();
        logger.LogInformation("Migration successful");
    }
}
catch (Exception exception)
{
    logger.LogError("Something went wrong during migration process. Please, try restarting the bot.");
    logger.LogError("If that won't help, don't hesitate to create issue on my GitHub page!");
    logger.LogError("Exception type: {exceptionType}", exception.GetType());
    logger.LogError("Exception message: {exceptionMessage}", exception.Message);
    return 1;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        var bot = services.AddHttpClient("owobot-csharp")
            .AddTypedClient<ITelegramBotClient>(
                typedClient => new TelegramBotClient(new TelegramBotClientOptions(configuration.GetSection("TELEGRAM_TOKEN").Value), 
                    typedClient));

        services.ConfigureOwobot(configuration, validator.GetProxy());
        services.AddTransient<IUpdateHandler, UpdateHandler>(); 
        services.AddTransient<IReceiverService, ReceiverService>();

        services.AddTransient<IHelperService, HelperService>()
            .AddLogging(cfg => cfg.AddConsole())
            .Configure<LoggerFilterOptions>(cfg => 
                cfg.MinLevel = LogLevel.Information); 
        services.AddDbContext<ApplicationContext>(); 
        services.AddHostedService<PollingService>();

        //Removing all logs with requests info due to privacy settings
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    })
    .Build();

await host.RunAsync();
return 0;




