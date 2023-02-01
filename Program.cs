using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using owobot_csharp;
using owobot_csharp.Data;
using owobot_csharp.Services;
using Telegram.Bot;

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

var proxy = validator.ProxyChecker();

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
    logger.LogError("Something went wrong. Please, restart the bot.");
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
        
        switch (proxy)
        {
            case "HTTP":
                bot.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { 
                        Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                            int.Parse(configuration.GetSection("PROXY_PORT").Value))
                        {
                            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                configuration.GetSection("PROXY_PASSWORD").Value)
                        }
                    });
                break;
            case "SOCKS5":
                bot.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                    {
                        Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                            int.Parse(configuration.GetSection("PROXY_PORT").Value))
                        {
                            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                configuration.GetSection("PROXY_PASSWORD").Value)
                        }
                    });
                break;
        }
        
        services.AddTransient<UpdateHandler>(); 
        services.AddTransient<ReceiverService>();
        services.AddTransient<IHelperService, HelperService>()
            .AddLogging(cfg => cfg.AddConsole())
            .Configure<LoggerFilterOptions>(cfg => 
                cfg.MinLevel = LogLevel.Information); 
        // services.AddSingleton<IHelperService, HelperService>()
        //     .AddLogging(cfg => cfg.AddConsole())
        //     .Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Information); 
        services.AddDbContext<ApplicationContext>(); 
        services.AddHostedService<PollingService>();

        //Removing all logs with requests info due to privacy settings
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    })
    .Build();

await host.RunAsync();
return 0;




