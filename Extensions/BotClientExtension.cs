using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using owobot_csharp.Folder;
using System.Net;
using Telegram.Bot;

namespace owobot_csharp.Extensions
{
    public static class BotClientExtension
    {
        public static IHttpClientBuilder ConfigureOwobot(this IServiceCollection services, IConfiguration configuration, Protocol? protocol = null)
        {
            return services.AddHttpClient("owobot-csharp")
                    .AddTypedClient<ITelegramBotClient>(httpClient =>
                    {
                        TelegramBotClientOptions options = new(configuration.GetSection("TELEGRAM_TOKEN").Value);
                        return new TelegramBotClient(options, httpClient);
                    })
                        .ConfigurePrimaryHttpMessageHandler(() => protocol switch
                        {
                            Protocol.Socks5 => new SocketsHttpHandler
                            {
                                Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                                    int.Parse(configuration.GetSection("PROXY_PORT").Value))
                                {
                                    Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                        configuration.GetSection("PROXY_PASSWORD").Value)
                                }
                            },
                            Protocol.Http => new HttpClientHandler
                            {
                                Proxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
                                int.Parse(configuration.GetSection("PROXY_PORT").Value))
                                {
                                    Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                                configuration.GetSection("PROXY_PASSWORD").Value)
                                }
                            },
                            _ => new HttpClientHandler()
                        });
        }
    }
}
