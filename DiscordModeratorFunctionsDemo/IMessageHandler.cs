using Discord.WebSocket;

namespace DiscordModeratorFunctionsDemo;
internal interface IMessageHandler
{
    Task HandleMessageAsync(SocketMessage message);
}