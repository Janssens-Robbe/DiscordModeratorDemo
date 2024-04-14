using Discord.WebSocket;

namespace DiscordModeratorDemo;
internal interface IMessageHandler
{
    Task HandleMessageAsync(SocketMessage message);
}