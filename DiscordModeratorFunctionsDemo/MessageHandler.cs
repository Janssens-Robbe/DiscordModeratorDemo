using Discord;
using Discord.WebSocket;
using DiscordModeratorFunctionsDemo.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace DiscordModeratorFunctionsDemo;
internal class MessageHandler : IMessageHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(IHttpClientFactory httpClientFactory, ILogger<MessageHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        var deleteRules = "The user may not praise that fries are french";
        var notifyRules = "Every message must always mention fries";

        var completionResponse = await GetChatCompletion(new ChatCompletionRequest
        {
            Model = "gpt-4-turbo",
            Messages =
            [
                new("system", "You are a chat moderator bot that enforces the rules."),
                new("user", message.Content)
            ],
            User = message.Author.Id.ToString(),
            MaxTokens = 2000,
            Temperature = 0.2,
            Tools =
            [
                new ChatCompletionTool
                {
                    Function = new ChatCompletionFunction
                    {
                        Name = "delete",
                        Parameters = new {
                            Type = "object",
                            Properties = new {
                                Reason = new {
                                    Type = "string"
                                }
                            },
                            Required = new[] { "reason" }
                        },
                        Description = $"You must call this function when the user violates the following rule(s): {deleteRules}"
                    }
                },
                new ChatCompletionTool
                {
                    Function = new ChatCompletionFunction
                    {
                        Name = "notify",
                        Parameters = new {
                            Type = "object",
                            Properties = new {
                                Reason = new {
                                    Type = "string"
                                }
                            },
                            Required = new[] { "reason" }
                        },
                        Description = $"You must call this function when the user violates the following rule(s): {notifyRules}"
                    }
                },
                new ChatCompletionTool
                {
                    Function = new ChatCompletionFunction
                    {
                        Name = "allow",
                        Description = "This is your default tool, you must call this function when the user does not violate any rules."
                    }
                }
            ]
        });

        if ((completionResponse.Choices[0].Message.ToolCalls?.Length ?? 0) == 0)
        {
            return;
        }

        var toolCall = completionResponse.Choices[0].Message.ToolCalls![0];

        if (toolCall.Function.Name == "allow")
        {
            return;
        }

        var arguments = JsonConvert.DeserializeObject<ChatCompletionFunctionArguments>(toolCall.Function.Arguments!)!;

        if (toolCall.Function.Name == "delete")
        {
            await HandleDisallowedMessage(message, arguments.Reason);
            return;
        }

        if (toolCall.Function.Name == "notify")
        {
            await NotifyModerators(message, arguments.Reason);
            return;
        }
    }

    private async Task<ChatCompletionResponse> GetChatCompletion(ChatCompletionRequest request)
    {
        using var client = _httpClientFactory.CreateClient(Constants.OpenAIHttpClientName);
        var requestBody = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });
        var result = await client.PostAsync("chat/completions", new StringContent(requestBody, Encoding.UTF8, "application/json"));
        result.EnsureSuccessStatusCode();
        var response = await result.Content.ReadAsStringAsync();
        var completionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(response)!;
        return completionResponse;
    }

    private async Task HandleDisallowedMessage(SocketMessage message, string? reason)
    {
        _logger.LogInformation("Deleting message from {Author} with id {id}", message.Author.Id, message.Id);
        await message.DeleteAsync();
        await message.Author.SendMessageAsync($"""
            Your message was deleted because it violated the rules.
            Reason: {reason}
            Message: ```{message.Content.Replace("`", "\\`")}```
            """);
    }

    private async Task NotifyModerators(SocketMessage message, dynamic reason)
    {
        _logger.LogInformation("Notifying moderators about message from {Author} with id {id}", message.Author.Id, message.Id);

        if (message.Channel is SocketGuildChannel guildChannel)
        {
            var logChannel = guildChannel.Guild.GetTextChannel(1229148593223630939);
            await logChannel.SendMessageAsync($"""
                User {message.Author.Mention} sent a message that violated the rules.
                Reason: {reason}
                Message: ```{message.Content.Replace("`", "\\`")}```
                """);
        }
    }

    private class ChatCompletionFunctionArguments
    {
        public required string Reason { get; init; }
    }
}
