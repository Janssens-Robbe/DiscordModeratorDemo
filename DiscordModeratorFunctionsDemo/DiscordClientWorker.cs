using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordModeratorFunctionsDemo;
internal class DiscordClientWorker : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DiscordClientWorker> _logger;

    public DiscordClientWorker(
        DiscordSocketClient client,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DiscordClientWorker> logger)
    {
        _client = client;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _configuration["Discord:Token"]);
        await _client.StartAsync();

        _client.Disconnected += async (e) =>
        {
            await Task.Delay(5000);
            await _client.StartAsync();
        };

        _client.Log += (logMessage) =>
        {
            var logLevel = logMessage.Severity switch
            {
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Error,
            };

            if (logMessage.Exception is not null)
            {
                _logger.Log(logLevel, "{Exception.GetType().FullName}: {Message}\n{Exception.StackTrace}", logMessage.Source, logMessage.Exception, logMessage.Message);
            }
            else
            {
                _logger.Log(logLevel, "{Source}: {Message}", logMessage.Source, logMessage.Message);
            }
            return Task.CompletedTask;
        };

        _client.MessageReceived += async (message) =>
        {
            if (message.Author.IsBot)
            {
                return;
            }

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            await messageHandler.HandleMessageAsync(message);
        };

        _client.MessageUpdated += async (oldMessage, newMessage, channel) =>
        {
            if (newMessage.Author.IsBot)
            {
                return;
            }

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            await messageHandler.HandleMessageAsync(newMessage);
        };
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }
}
