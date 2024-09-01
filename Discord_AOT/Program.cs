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
        case not null when c.Data.CustomId.StartsWith("bftp:share"):
        {
            await ProcessMessage(c, AttachmentItem.FromBase64(c.Data.CustomId.Split(":")[2]), ephemeral: false);
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

async Task ProcessMessage(SocketInteraction interaction, AttachmentItem item, bool ephemeral = true)
{
    var message = await interaction.Channel.GetMessageAsync(item.MessageId);
    if (message == null)
    {
        await interaction.RespondAsync("Message not found!", ephemeral: true);
        return;
    }

    var replyEmbed = new EmbedBuilder()
        .WithAuthor(message.Author)
        .WithTimestamp(message.Timestamp)
        .WithUrl(
            $"https://discord.com/channels/{interaction.GuildId}/{item.ChannelId}/{item.MessageId}");
    var component = new ComponentBuilder()
        .WithButton("Jump!", style: ButtonStyle.Link, url:
            $"https://discord.com/channels/{interaction.GuildId}/{item.ChannelId}/{item.MessageId}")
        .WithButton("More please!", "bftp:generate", style: ButtonStyle.Success, emote: Emote.Parse("<a:Vbongo:971453554076299294>"));
    if (ephemeral) component.WithButton("Share!", $"bftp:share:{item.ToBase64()}", emote: Emote.Parse("<a:snappi_coffee:1249258418946969650>"));

    if (item.AttachmentId > 1000)
    {
        // Attachment
        var attachment = message.Attachments.First(a => a.Id == item.AttachmentId);
        await interaction.RespondAsync(
            embed: replyEmbed
                .WithTitle(attachment.Filename)
                .WithImageUrl(attachment.Url)
                .WithFooter(attachment.ContentType)
                .Build(),
            ephemeral: ephemeral,
            components: component.Build());
    }
    else
    {
        // Embed
        var embed = message.Embeds.ElementAt((int)item.AttachmentId);
        await interaction.RespondAsync(
            embed: replyEmbed
                .WithTitle(embed.Title)
                .WithImageUrl(EmbedTools.GetUrl(embed)!.ToString())
                .WithFooter(embed.Type.ToString())
                .Build(),
            ephemeral: ephemeral,
            components: component.Build());
    }
}

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

        if (await interaction.Channel.GetMessageAsync(messageId) == null)
            continue;

        await ProcessMessage(interaction, new AttachmentItem
            { ChannelId = interaction.ChannelId.Value, MessageId = messageId, AttachmentId = attachmentId });

        break;
    }
}