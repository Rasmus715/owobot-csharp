using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using owobot_csharp.Exceptions;
using owobot_csharp.Folder;

namespace owobot_csharp;

public class Validator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    public Validator(IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger("Validator");
    }
    public void Validate()
    {
        var parseSuccessful = true;

        if (!string.IsNullOrEmpty(_configuration.GetSection("TELEGRAM_TOKEN")?.Value))
        {
            _logger.LogError("Telegram Token field is not present.");
            parseSuccessful = false;
        }
        
        if (!string.IsNullOrEmpty(_configuration.GetSection("BOT_VERSION")?.Value))
        {
            Environment.SetEnvironmentVariable("BOT_VERSION", "1.0.1");
        }

        if (_configuration.GetSection("REDDIT_APP_ID").Exists() || 
            _configuration.GetSection("REDDIT_SECRET").Exists() || 
            _configuration.GetSection("REDDIT_REFRESH_TOKEN").Exists())
        {
            if (!string.IsNullOrEmpty(_configuration.GetSection("REDDIT_APP_ID")?.Value))
            {
                _logger.LogError("Reddit App ID is not present.");
                parseSuccessful = false;
            }

            if (!string.IsNullOrEmpty(_configuration.GetSection("REDDIT_REFRESH_TOKEN")?.Value))
            { 
                _logger.LogError("Reddit Refresh Token is not present.");
                parseSuccessful = false;
            }

            if (!string.IsNullOrEmpty(_configuration.GetSection("REDDIT_SECRET")?.Value))
            {  
                _logger.LogError("Reddit Secret Key is not present.");
                parseSuccessful = false; 
            } 
        }
        
        if (_configuration.GetSection("PROXY").Exists()) 
        { 
            if (_configuration.GetSection("PROXY").Value.Equals("HTTP") || 
                _configuration.GetSection("PROXY").Value.Equals("SOCKS5")) 
            { 
                if (!string.IsNullOrEmpty(_configuration.GetSection("PROXY_ADDRESS")?.Value)) 
                { 
                    _logger.LogError("Proxy field is filled but no address was provided."); 
                    parseSuccessful = false; 
                }
                
                if (!string.IsNullOrEmpty(_configuration.GetSection("PROXY_PORT")?.Value)) 
                {
                    _logger.LogError("Proxy field is filled but no port was provided."); 
                    parseSuccessful = false; 
                } 
            }
            else 
            { 
                _logger.LogError(@"Proxy field is filled with unsupported value. Valid values are ""HTTP"", ""SOCKS5"""); 
                parseSuccessful = false; 
            } 
        }

        if (!parseSuccessful) 
            throw new ValidationException();
        
        _logger.LogInformation("Configuration looks OK.");
    }

    public Protocol? GetProxy()
    {
        if (Enum.TryParse(_configuration.GetSection("PROXY").Value, true, out Protocol protocol).Equals(false))
            return null;

        _logger.LogInformation("Using {protocol} proxies, huh? Cool...", protocol);
        _logger.LogInformation("I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");
        return protocol;
    }
}