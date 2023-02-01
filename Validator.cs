using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        if (_configuration.GetSection("TELEGRAM_TOKEN")?.Value is null or "")
        {
            _logger.LogError("Telegram Token field is not present.");
            parseSuccessful = false;
        }
        
        if (_configuration.GetSection("BOT_VERSION")?.Value is null or "")
        {
            Environment.SetEnvironmentVariable("BOT_VERSION", "1.0.0");
        }

        if (_configuration.GetSection("REDDIT_APP_ID").Exists() || 
            _configuration.GetSection("REDDIT_SECRET").Exists() || 
            _configuration.GetSection("REDDIT_REFRESH_TOKEN").Exists())
        {
            if (_configuration.GetSection("REDDIT_APP_ID").Value.Equals(""))
            {
                _logger.LogError("Reddit Secret is not present.");
                parseSuccessful = false;
            }

            if (_configuration.GetSection("REDDIT_REFRESH_TOKEN").Value.Equals(""))
            { 
                _logger.LogError("Reddit Refresh Token is not present.");
                parseSuccessful = false;
            }

            if (_configuration.GetSection("REDDIT_SECRET").Value.Equals(""))
            {  
                _logger.LogError("Reddit Secret is not present.");
                parseSuccessful = false; 
            } 
        }
        
        if (_configuration.GetSection("PROXY").Exists()) 
        { 
            if (_configuration.GetSection("PROXY").Value.Equals("HTTP") || 
                _configuration.GetSection("PROXY").Value.Equals("SOCKS5")) 
            { 
                if (_configuration.GetSection("PROXY_ADDRESS")?.Value is null or "") 
                { 
                    _logger.LogError("Proxy field is present but no address was provided."); 
                    parseSuccessful = false; 
                }
                
                if (_configuration.GetSection("PROXY_PORT")?.Value is null or "") 
                {
                    _logger.LogError("Proxy field is present but no port was provided."); 
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

    public string ProxyChecker()
    {
        if (_configuration.GetSection("PROXY").Value?.Equals("HTTP") ?? false)
        {
            _logger.LogInformation("Using HTTP proxies, huh? Cool...");
            _logger.LogInformation(
                "I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");
            return "HTTP";
        }

        if (_configuration.GetSection("PROXY").Value?.Equals("SOCKS5") ?? false) 
        {
            _logger.LogInformation(@"Using SOCKS5 proxies, huh? Cool..."); 
            _logger.LogInformation(
                @"I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");
            return "SOCKS5";
            
        }
        
        return "NO PROXY";
    }
    
}

public class ValidationException : Exception
{
    
}