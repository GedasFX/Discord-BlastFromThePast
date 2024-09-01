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
        .AddOption(new SlashCommandOptionBuilder()
            .WithType(ApplicationCommandOptionType.SubCommand)
            .WithName("random")
            .WithDescription("Display a random thing from the past"))
        .Build();

    var guild = discord.GetGuild(BotConfig.Instance.GuildId);
    await guild.CreateApplicationCommandAsync(command);
};

discord.ButtonExecuted += async c =>
{
    switch (c.Data.CustomId)
    {
        case "bftp:generate":
        {
            await ProcessRandomMessage(c);
            break;
        }
    }
};
discord.SlashCommandExecuted += async command =>
{
    if (command.Data.Name == "bftp")
    {
        var subcommand = command.Data.Options.ElementAt(0)!;
        switch (subcommand.Name)
        {
            case "import":
            {
                if (command.User is not IGuildUser { GuildPermissions.Administrator: true })
                    return;

                await command.RespondAsync("Starting Import...", ephemeral: true);

                _ = Task.Run(async () =>
                {
                    using var importer = new ImageImporter(discord.GetGuild(BotConfig.Instance.GuildId)
                        .GetTextChannel(command.ChannelId!.Value));
                    await importer.ImportAsync();

                    await command.FollowupAsync("Import complete!", ephemeral: true);
                });

                break;
            }
            case "random":
            {
                await ProcessRandomMessage(command);
                break;
            }
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
return;

async Task ProcessRandomMessage(SocketInteraction interaction)
{
    var count = Directory.EnumerateFiles($"images/{interaction.ChannelId!.Value}").Count();

    while (true)
    {
        var random = Directory.EnumerateFiles($"images/{interaction.ChannelId!.Value}")
            .ElementAt(Random.Shared.Next(count));

        var file = new FileInfo(random);

        var parts = file.Name.Split('_');
        var messageId = ulong.Parse(parts[0]);
        var attachmentId = ulong.Parse(parts[1]);

        var message = await interaction.Channel.GetMessageAsync(messageId);
        if (message == null)
            continue;

        var replyEmbed = new EmbedBuilder()
            .WithAuthor(message.Author)
            .WithTimestamp(message.Timestamp)
            .WithUrl(
                $"https://discord.com/channels/{interaction.GuildId}/{interaction.ChannelId}/{messageId}");
        var component = new ComponentBuilder()
            .WithButton("Jump!", style: ButtonStyle.Link, url:
                $"https://discord.com/channels/{interaction.GuildId}/{interaction.ChannelId}/{messageId}")
            .WithButton("More please! <3", "bftp:generate", style: ButtonStyle.Success);

        if (attachmentId > 1000)
        {
            // Attachment
            var attachment = message.Attachments.First(a => a.Id == attachmentId);
            await interaction.RespondAsync(
                embed: replyEmbed
                    .WithTitle(attachment.Filename)
                    .WithImageUrl(attachment.Url)
                    .WithFooter(attachment.ContentType)
                    .Build(),
                ephemeral: true,
                components: component.Build());
        }
        else
        {
            // Embed
            var embed = message.Embeds.ElementAt((int)attachmentId);
            await interaction.RespondAsync(embed: replyEmbed
                    .WithTitle(embed.Title)
                    .WithImageUrl(EmbedTools.GetUrl(embed)!.ToString())
                    .WithFooter(embed.Type.ToString())
                    .Build(), ephemeral: true,
                components: component.Build());
        }


        break;
    }
}