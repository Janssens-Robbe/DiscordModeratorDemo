using Discord;
using Discord.WebSocket;
using DiscordModeratorDemo.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace DiscordModeratorDemo;
internal class MessageHandler : IMessageHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessageHandler> _logger;
    private const string _rules = """
        * The user must mention fries
        * The user may not praise "french fries"
        """;

    public MessageHandler(IHttpClientFactory httpClientFactory, ILogger<MessageHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        var completionResponse = await GetChatCompletion(new ChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages =
            [
                new ("system", $"""
                    You are a chat moderator that enforces the following rules:
                    {_rules}

                    Answer with allowed or disallowed
                    """),
                new("user", message.Content)
            ],
            User = message.Author.Id.ToString(),
            MaxTokens = 2,
            Temperature = 0,
            LogitBias = new Dictionary<int, int>
            {
                [21642] = 100, // "allowed"
                [4338] = 100, // "dis"
                [13] = 100 // "."
            },
            Stop = ["."]
        });

        if (completionResponse.Choices[0].Message.Content == "allowed")
        {
            return;
        }

        await HandleDisallowedMessage(message, completionResponse.Choices[0].Message.Content);
    }

    private async Task<ChatCompletionResponse> GetChatCompletion(ChatCompletionRequest request)
    {
        using var client = _httpClientFactory.CreateClient(Constants.OpenAIHttpClientName);
        var requestBody = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var result = await client.PostAsync("chat/completions", new StringContent(requestBody, Encoding.UTF8, "application/json"));
        result.EnsureSuccessStatusCode();
        var response = await result.Content.ReadAsStringAsync();
        var completionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(response)!;
        return completionResponse;
    }

    private async Task HandleDisallowedMessage(SocketMessage message, string? initalResponse)
    {
        _logger.LogInformation("Deleting message from {Author} with id {id}", message.Author.Id, message.Id);
        await message.DeleteAsync();
        await SendFeedbackToAuthor(message, initalResponse);
    }

    private async Task SendFeedbackToAuthor(SocketMessage message, string? initalResponse)
    {
        var completionResponse = await GetChatCompletion(new ChatCompletionRequest
        {
            Model = "gpt-3.5-turbo",
            Messages =
            [
                new ("system", $"""
                    You are a chat moderator that enforces the following rules:
                    {_rules}

                    Answer with the reason
                    """),
                new("user", message.Content),
                new("assistant", initalResponse),
                new("user", "Why?")
            ],
            User = message.Author.Id.ToString(),
            MaxTokens = 50
        });

        await message.Author.SendMessageAsync($"""
            Your message was deleted because it violated the rules.
            Reason: {completionResponse.Choices[0].Message.Content}
            Message: ```{message.Content.Replace("`", "\\`")}```
            """);
    }
}
