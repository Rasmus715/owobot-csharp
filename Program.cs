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
using Singularity;
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
    var pendingMigrations = await applicationContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        logger.LogInformation("Initializing migration...");
        await applicationContext.Database.MigrateAsync();
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
    .UseSingularity()
    .ConfigureContainer<ContainerBuilder>(builder => 
    {
        builder.Register<IUpdateHandler, UpdateHandler>(configuration => configuration.With(Lifetimes.Transient));
        builder.Register<IReceiverService, ReceiverService>(configuration => configuration.With(Lifetimes.Transient));
        builder.Register<IHelperService, HelperService>(configuration => configuration.With(Lifetimes.Transient));
        builder.Register<DbContext, ApplicationContext>(configuration => configuration.With(Lifetimes.PerScope));
    })
    .ConfigureServices(services =>
    {
        services.ConfigureOwobot(configuration, validator.GetProxy());
        services.AddHostedService<PollingService>();
        //Removing all logs with requests info due to privacy settings
        services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    })
    .Build();
await host.RunAsync();
return 0;




