using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace DiscordModeratorDemo.Models;
internal class ChatCompletionRequest
{
    public string Model { get; init; } = "gpt-3.5-turbo";
    public required ChatMessage[] Messages { get; init; }
    public string? User { get; init; }

    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; init; }

    public double? Temperature { get; init; }

    [JsonProperty("logit_bias")]
    public Dictionary<int, int>? LogitBias { get; init; }

    public string[]? Stop { get; init; }
}

internal class ChatMessage
{
    public ChatMessage() { }

    [SetsRequiredMembers]
    public ChatMessage(string role, string? content)
    {
        Role = role;
        Content = content;
    }

    public required string Role { get; init; }
    public string? Content { get; init; }
}
