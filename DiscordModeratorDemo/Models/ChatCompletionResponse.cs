using Newtonsoft.Json;

namespace DiscordModeratorDemo.Models;
internal class ChatCompletionResponse
{
    public required string Id { get; init; }
    public required string Object { get; init; } // Should be "chat.completion"
    public int Created { get; init; }
    public required string Model { get; init; }

    [JsonProperty("system_fingerprint")]
    public required string SystemFingerprint { get; init; }
    public required ChatCompletionResponseChoice[] Choices { get; init; }
    public required ChatCompletionResponseUsage Usage { get; init; }

    public class ChatCompletionResponseUsage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; init; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; init; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; init; }
    }

    public class ChatCompletionResponseChoice
    {
        public int Index { get; init; }
        public required ChatCompletionResponseMessage Message { get; init; }

        [JsonProperty("finish_reason")]
        public required string FinishReason { get; init; }
    }

    public class ChatCompletionResponseMessage
    {
        public required string Role { get; init; }
        public string? Content { get; init; }
    }
}
