using Discord;
using Discord_AOT;
using Discord.WebSocket;

var discord = new DiscordSocketClient(new DiscordSocketConfig()
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});

discord.Ready += async () =>
{
    var command = new SlashCommandBuilder()
        .WithName("bftp")
        .WithDescription("Blast From The Past")
        .AddOption(new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.SubCommand)
            .WithName("import")
            .WithDescription("Import Images from the current channel"))
        .Build();

    var guild = discord.GetGuild(BotConfig.Instance.GuildId);
    await guild.CreateApplicationCommandAsync(command);
};

discord.SlashCommandExecuted += async command =>
{
    if (command.Data.Name == "bftp")
    {
        var subcommand = command.Data.Options.ElementAt(0)!;
        if (subcommand.Name == "import")
        {
            await command.RespondAsync("Starting Import...", ephemeral: true);

            _ = Task.Run(async () =>
            {
                using var importer = new ImageImporter(discord.GetGuild(BotConfig.Instance.GuildId)
                    .GetTextChannel(command.ChannelId!.Value));
                await importer.ImportAsync();

                await command.FollowupAsync("Import complete!", ephemeral: true);
            });
        }
    }
};

discord.Log += message =>
{
    if (message.Exception == null)
        return Console.Out.WriteLineAsync($"{message.Severity:G} {message.Message}");
    return Console.Out.WriteLineAsync($"{message.Severity:G} {message.Message}\n{message.Exception}");
};

await discord.LoginAsync(TokenType.Bot, BotConfig.Instance.Token);
await discord.StartAsync();

await Task.Delay(-1);