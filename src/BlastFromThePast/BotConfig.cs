namespace BlastFromThePast;

public class BotConfig
{
    public static BotConfig Instance { get; } = GetConfig();

    public required string Token { get; set; }
    public ulong GuildId { get; set; }

    public static BotConfig GetConfig()
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ??
                    throw new Exception("Missing DISCORD_TOKEN environment variable");

        var guildId = Environment.GetEnvironmentVariable("DISCORD_GUILD_ID") ??
                      throw new Exception("Missing DISCORD_GUILD_ID environment variable");

        return new BotConfig()
        {
            Token = token,
            GuildId = ulong.TryParse(guildId, out var v) && v > 0
                ? v
                : throw new Exception("DISCORD_GUILD_ID is missing a value")
        };
    }
}