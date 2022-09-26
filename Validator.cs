using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace owobot_csharp;

public static class Validator
{
    public static bool Validate(string[] args)
    {
        var logger = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger("Validator");
        
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        
        var parseSuccessful = true;

        if (!configuration.GetSection("TELEGRAM_TOKEN").Exists() ||
            configuration.GetSection("TELEGRAM_TOKEN").Value.Equals(""))
        {
            logger.LogError("Telegram Token field is not present.");
            parseSuccessful = false;
        }
        
        if (!configuration.GetSection("BOT_VERSION").Exists() ||
            configuration.GetSection("BOT_VERSION").Value.Equals(""))
        {
            logger.LogError("Bot Version field is not present.");
            parseSuccessful = false;
        }

        if (configuration.GetSection("REDDIT_APP_ID").Exists() || configuration.GetSection("REDDIT_SECRET").Exists() || 
            configuration.GetSection("REDDIT_REFRESH_TOKEN").Exists())
        {
            if (configuration.GetSection("REDDIT_APP_ID").Value.Equals(""))
            {
                logger.LogError("Reddit Secret is not present.");
                parseSuccessful = false;
            }

            if (configuration.GetSection("REDDIT_REFRESH_TOKEN").Value.Equals(""))
            { 
                logger.LogError("Reddit Refresh Token is not present.");
                parseSuccessful = false;
            }

            if (configuration.GetSection("REDDIT_REFRESH_TOKEN").Value.Equals(""))
            {  
                logger.LogError("Reddit Refresh Token is not present.");
                parseSuccessful = false; 
            } 
        }
        
        if (configuration.GetSection("PROXY").Exists()) 
        { 
            if (configuration.GetSection("PROXY").Value.Equals("HTTP") || 
                configuration.GetSection("PROXY").Value.Equals("SOCKS5")) 
            { 
                if (!configuration.GetSection("PROXY_ADDRESS").Exists() || 
                    configuration.GetSection("PROXY_ADDRESS").Value.Equals("")) 
                { 
                    logger.LogError("Proxy field is present but no address was provided."); 
                    parseSuccessful = false; 
                }
                
                if (!configuration.GetSection("PROXY_PORT").Exists() ||
                    configuration.GetSection("PROXY_PORT").Value.Equals("")) 
                {
                    logger.LogError("Proxy field is present but no port was provided."); 
                    parseSuccessful = false; 
                } 
            }
            else 
            { 
                logger.LogError(@"Proxy field is filled with unsupported value. Valid values are ""HTTP"", ""SOCKS5"""); 
                parseSuccessful = false; 
            } 
        }
        
        if (parseSuccessful) 
        { 
            logger.LogInformation("Configuration looks OK.");
            return false;
        }
        
        logger.LogError("Please, fix the errors listed above and try again"); 
        return true; 
    }

    public static string ProxyChecker(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        
        var logger = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger("Validator");

        if (!configuration.GetSection("PROXY").Exists()) return null;
        if (configuration.GetSection("PROXY").Value.Equals("HTTP"))
        {
            logger.LogInformation(@"Using HTTP proxies, huh? Cool...");
            logger.LogInformation(
                @"I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");

            return "HTTP";
        }

        if (!configuration.GetSection("PROXY").Value.Equals("SOCKS5")) return null;
        logger.LogInformation(@"Using SOCKS5 proxies, huh? Cool...");
        logger.LogInformation(
            @"I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");

        return "SOCKS5";
    }
}