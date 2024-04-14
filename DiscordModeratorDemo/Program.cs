using Discord;
using Discord.WebSocket;
using DiscordModeratorDemo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.GuildMessages | GatewayIntents.Guilds
        };
        services.AddSingleton(discordSocketConfig);
        services.AddSingleton<DiscordSocketClient>();
        services.AddHostedService<DiscordClientWorker>();
        services.AddHttpClient(Constants.OpenAIHttpClientName, (provider, client) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config["OpenAI:ApiKey"]}");
        });
        services.AddScoped<IMessageHandler, MessageHandler>();
    });
var host = builder.Build();
await host.RunAsync();