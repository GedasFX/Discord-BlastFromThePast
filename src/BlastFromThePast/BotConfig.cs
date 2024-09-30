using System.Collections.Immutable;

namespace BlastFromThePast;

public class BotConfig
{
    public static BotConfig Instance { get; } = GetConfig();

    public required string Token { get; init; }
    public ImmutableArray<ulong> HostGuildIds { get; init; }

    private static BotConfig GetConfig()
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ??
                    throw new Exception("Missing DISCORD_TOKEN environment variable");

        var guildIds = Environment.GetEnvironmentVariable("ARCHIVE_DISCORD_GUILD_IDS")?
            .Split(',')
            .Select(e =>
            {
                _ = ulong.TryParse(e, out var q);
                return q;
            })
            .Where(q => q > 0)
            .ToImmutableArray() ?? [];

        return new BotConfig()
        {
            Token = token,
            HostGuildIds = guildIds,
        };
    }
}