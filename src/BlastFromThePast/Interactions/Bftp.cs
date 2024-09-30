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
        public async Task ProcessRandomCommand(
            [Summary(description: "Channel to use. Default uses current channel.")]
            IMessageChannel? channel = null)
        {
            await ProcessRandom(channel ?? Context.Channel);
        }

        [ComponentInteraction("bftp:generate:*", ignoreGroupNames: true)]
        public async Task ProcessRandomComponent(ulong channelId)
        {
            await ProcessRandom(Context.Guild.GetTextChannel(channelId));
        }

        [ComponentInteraction("bftp:share:*", ignoreGroupNames: true)]
        public async Task ProcessShareComponent(string uuid)
        {
            await Process(AttachmentItem.FromBase64(uuid), ephemeral: false);
        }

        [MessageCommand("BFTP: Share")]
        public async Task ProcessShareMessageCommand(IMessage message)
        {
            var attachment = message.GetAttachmentItems(Context.Guild.Id).FirstOrDefault();
            if (attachment == default)
            {
                await RespondAsync("Message has no attachments", ephemeral: true);
                return;
            }

            await Process(attachment.Attachment, ephemeral: false);
        }

        [MessageCommand("BFTP: Save")]
        public async Task ProcessSaveMessageComponent(IMessage message)
        {
            var attachment = message.GetAttachmentItems(Context.Guild.Id).FirstOrDefault();
            if (attachment == default)
            {
                await RespondAsync("Message has no attachments", ephemeral: true);
                return;
            }

            var (embed, _) = GetEmbed(message, attachment.Attachment);

            await RespondAsync("Sent image to DMs", ephemeral: true);
            await Context.User.SendMessageAsync(embed: embed.Build());
        }

        private async Task ProcessRandom(IMessageChannel channel, bool ephemeral = true)
        {
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
                    await Process(item, ephemeral);
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

        private async Task Process(AttachmentItem item, bool ephemeral = true)
        {
            var message = await Context.Client.GetGuild(item.GuildId).GetTextChannel(item.ChannelId)
                .GetMessageAsync(item.MessageId);
            if (message == null)
                throw new MessageNotFoundException();

            var (embed, component) = GetEmbed(message, item, ephemeral);
            await RespondAsync(embed: embed.Build(), ephemeral: ephemeral, components: component.Build());
        }

        private static (EmbedBuilder embed, ComponentBuilder component) GetEmbed(IMessage message,
            AttachmentItem item, bool ephemeral = true)
        {
            var uuid = item.ToBase64();

            var replyEmbed = new EmbedBuilder()
                .WithAuthor(message.Author)
                .WithTimestamp(message.Timestamp)
                .WithUrl($"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.MessageId}")
                .AddField("Snatched it here:",
                    $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.MessageId}");

            var component = new ComponentBuilder()
                .WithButton("Jump!", style: ButtonStyle.Link, url:
                    $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.MessageId}");

            component.WithButton("More please!", $"bftp:generate:{item.ChannelId}", style: ButtonStyle.Success,
                emote: Emote.Parse("<a:Vbongo:971453554076299294>"));
            if (ephemeral)
            {
                component.WithButton("Share!", $"bftp:share:{uuid}",
                    emote: Emote.Parse("<a:snappi_coffee:1249258418946969650>"));
            }


            if (item.AttachmentId > 1000)
            {
                // Attachment
                var attachment = message.Attachments.First(a => a.Id == item.AttachmentId);
                return (
                    replyEmbed
                        .WithTitle(attachment.Filename)
                        .WithFooter(attachment.ContentType)
                        .WithImageUrl(attachment.Url),
                    component);
            }

            // Embed
            var embed = message.Embeds.ElementAt((int)item.AttachmentId);
            return (
                replyEmbed
                    .WithTitle(new Uri(embed.Url).Authority)
                    .WithFooter(embed.Type.ToString())
                    .WithImageUrl(EmbedTools.GetUrl(embed)!.ToString()),
                component);
        }
    }
}