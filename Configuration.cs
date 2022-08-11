using Newtonsoft.Json;

namespace owobot_csharp;
public class Configuration
{
    [JsonProperty("BotVersion")]
    public string? BotVersion { get; set; }

    [JsonProperty("TelegramToken")]
    public string? TelegramToken { get; set; }

    [JsonProperty("RedditAppId")]
    public string? RedditAppId { get; set; }

    [JsonProperty("RedditSecret")]
    public string? RedditSecret { get; set; }

    [JsonProperty("RedditRefreshToken")]
    public string? RedditRefreshToken { get; set; }

    [JsonProperty("ProxyHTTP")]
    public Proxy? ProxyHttp { get; set; }

    [JsonProperty("ProxySOCKS5")]
    public Proxy? ProxySocks5 { get; set; }
}

public class Proxy
{
    [JsonProperty("Address")]
    public string? Address { get; set; }

    [JsonProperty("Port")]
    public string? Port { get; set; }

    [JsonProperty("Username")]
    public string? Username { get; set; }

    [JsonProperty("Password")]
    public string? Password { get; set; }
}