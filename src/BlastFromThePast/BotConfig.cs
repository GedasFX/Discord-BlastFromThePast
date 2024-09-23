using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlastFromThePast;

public class BotConfig
{
    public static BotConfig Instance { get; } = GetConfig("config.json");

    public required string Token { get; set; }
    public ulong GuildId { get; set; }

    public static BotConfig GetConfig(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, SourceGenerationContext.Default.BotConfig)!;
    }
}

[JsonSerializable(typeof(BotConfig))]
internal partial class SourceGenerationContext : JsonSerializerContext;