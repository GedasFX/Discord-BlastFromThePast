using BlastFromThePast.Data;
using BlastFromThePast.Data.Models;
using BlastFromThePast.Exceptions;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastFromThePast.Interactions;

[Group("bftp", "Blast from the Past")]
public class Bftp(IServiceProvider serviceProvider) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("import", "Import Images from the specified channel.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Import(
        [Summary(description: "Channel to use. Default uses current channel.")]
        IMessageChannel? channel = null)
    {
        await RespondAsync("Starting Import...", ephemeral: true);

        var importer = serviceProvider.GetRequiredService<ImageImporter>();
        await importer.ImportAsync(Context.Guild.Id, channel ?? Context.Channel,
            BotConfig.Instance.HostGuildIds.IndexOf(Context.Guild.Id) >= 0);

        await FollowupAsync("Import complete!", ephemeral: true);
    }

    [Group("show", "Displays an image from the channel.")]
    public class Show : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("random", "Display a random image.")]
        public async Task Random(
            [Summary(description: "Channel to use. Default uses current channel.")]
            IMessageChannel? channel = null)
        {
            channel ??= Context.Channel;

            while (true)
            {
                AttachmentItem? item;
                await using (var dbContext = new AppDbContext())
                {
                    item = await dbContext.Items.AsNoTracking()
                        .Where(i => i.GuildId == Context.Guild.Id && i.ChannelId == channel.Id)
                        .OrderBy(o => EF.Functions.Random())
                        .FirstOrDefaultAsync();
                }

                if (item == null)
                {
                    await RespondAsync("No images imported on the channel. Try running `/bftp import`.");
                    return;
                }

                try
                {
                    await Process(item);
                    return;
                }
                catch (MessageNotFoundException)
                {
                    // Deleted
                    await using var dbContext = new AppDbContext();
                    await dbContext.Items.AsNoTracking()
                        .Where(i =>
                            i.GuildId == Context.Guild.Id &&
                            i.ChannelId == channel.Id &&
                            i.MessageId == item.MessageId)
                        .ExecuteDeleteAsync();
                }
            }
        }

        [SlashCommand("by-id", "Display a specific image.")]
        public async Task ById(
            [Summary(description: "Unique image identifier.")]
            string uuid)
        {
            await Process(AttachmentItem.FromBase64(uuid));
        }

        private async Task Process(AttachmentItem item, bool ephemeral = true)
        {
            var message = await Context.Client.GetGuild(item.GuildId).GetTextChannel(item.ChannelId)
                .GetMessageAsync(item.MessageId);
            if (message == null)
                throw new MessageNotFoundException();

            var uuid = item.ToBase64();

            var replyEmbed = new EmbedBuilder()
                .WithAuthor(message.Author)
                .WithTimestamp(message.Timestamp)
                .WithUrl(
                    $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.MessageId}")
                .WithFooter(uuid);

            var component = new ComponentBuilder()
                .WithButton("Jump!", style: ButtonStyle.Link, url:
                    $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.MessageId}")
                .WithButton("More please!", "bftp:generate", style: ButtonStyle.Success,
                    emote: Emote.Parse("<a:Vbongo:971453554076299294>"));
            if (ephemeral)
                component.WithButton("Share!", $"bftp:share:{uuid}",
                    emote: Emote.Parse("<a:snappi_coffee:1249258418946969650>"));

            if (item.AttachmentId > 1000)
            {
                // Attachment
                var attachment = message.Attachments.First(a => a.Id == item.AttachmentId);
                await RespondAsync(
                    embed: replyEmbed
                        .WithTitle(attachment.Filename)
                        .WithImageUrl(attachment.Url)
                        .Build(),
                    ephemeral: ephemeral,
                    components: component.Build());
            }
            else
            {
                // Embed
                var embed = message.Embeds.ElementAt((int)item.AttachmentId);
                await RespondAsync(
                    embed: replyEmbed
                        .WithTitle(embed.Title)
                        .WithImageUrl(EmbedTools.GetUrl(embed)!.ToString())
                        .Build(),
                    ephemeral: ephemeral,
                    components: component.Build());
            }
        }
    }
}