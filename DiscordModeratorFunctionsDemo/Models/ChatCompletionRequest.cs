using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace DiscordModeratorFunctionsDemo.Models;
internal class ChatCompletionRequest
{
    public string Model { get; init; } = "gpt-3.5-turbo";
    public required ChatMessage[] Messages { get; init; }
    public string? User { get; init; }

    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; init; }

    public double? Temperature { get; init; }

    public ChatCompletionTool[]? Tools { get; init; }
}

internal class ChatCompletionTool
{
    public string Type { get; init; } = "function";
    public required ChatCompletionFunction Function { get; init; }
}

internal class ChatCompletionFunction
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public object? Parameters { get; init; }
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
