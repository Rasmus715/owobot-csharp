using System.Net;
using BooruSharp.Booru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using owobot_csharp.Data;
using owobot_csharp.Services;
using Telegram.Bot;
using static owobot_csharp.Validator;

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("Program");

if (Validate(args))
    return 1;

var proxy = ProxyChecker(args);

IConfiguration configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

logger.LogInformation("Initializing migration...");

if (!Directory.Exists("Essentials"))
{
    logger.LogInformation(@"Creating ""Essentials"" Directory");
    Directory.CreateDirectory("Essentials");
}

try
{
    var applicationContext = new ApplicationContext();
    applicationContext.Database.Migrate();
    applicationContext.Dispose();
}
catch (Exception)
{
    logger.LogError("Something went wrong. Please, restart the bot");
    return 1;
}


logger.LogInformation(@"Migration successful");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        switch (proxy)
        {
            case "HTTP":
                services.AddHttpClient("owobot-csharp")
                    .AddTypedClient<ITelegramBotClient>(httpClient =>
                    {
                        TelegramBotClientOptions options = new(configuration.GetSection("TELEGRAM_TOKEN").Value);
                        return new TelegramBotClient(options, httpClient);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { 
                        Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                            int.Parse(configuration.GetSection("PROXY_PORT").Value))
                        {
                            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                configuration.GetSection("PROXY_PASSWORD").Value)
                        }
                    });
                break;
            case "SOCKS5":
                services.AddHttpClient("owobot-csharp")
                    .AddTypedClient<ITelegramBotClient>(httpClient =>
                    {
                        TelegramBotClientOptions options = new(configuration.GetSection("TELEGRAM_TOKEN").Value);
                        return new TelegramBotClient(options, httpClient);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                    {
                        Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                            int.Parse(configuration.GetSection("PROXY_PORT").Value))
                        {
                            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                configuration.GetSection("PROXY_PASSWORD").Value)
                        }
                    });
                break;
            default:
                services.AddHttpClient("owobot-csharp")
                    .AddTypedClient<ITelegramBotClient>(httpClient =>
                    {
                        TelegramBotClientOptions options = new(configuration.GetSection("TELEGRAM_TOKEN").Value);
                        return new TelegramBotClient(options, httpClient);
                    });
                break;
        }
        
        services.AddTransient<UpdateHandler>(); 
        services.AddTransient<ReceiverService>(); 
        services.AddTransient<IHelperService, HelperService>()
            .AddLogging(cfg => cfg.AddConsole())
            .Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Information); 
        services.AddDbContext<ApplicationContext>(); 
        services.AddHostedService<PollingService>();

        //Removing all logs with requests info due to privacy settings
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    })
    .Build();

await host.RunAsync();
return 0;




