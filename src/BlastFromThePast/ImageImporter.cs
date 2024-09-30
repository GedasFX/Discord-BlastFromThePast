using System.Web;
using BlastFromThePast.Data;
using BlastFromThePast.Data.Models;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace BlastFromThePast;

public static class EmbedTools
{
    public static Uri? GetUrl(IEmbed embed)
    {
        switch (embed.Type)
        {
            case EmbedType.Image:
            case EmbedType.Video when embed.Provider is { Name: "YouTube" }:
            case EmbedType.Link when embed.Thumbnail.HasValue:
            case EmbedType.Article when embed.Thumbnail.HasValue:
                return new Uri(embed.Thumbnail!.Value.Url);
            case EmbedType.Article when embed.Image.HasValue:
                return new Uri(embed.Image.Value.Url);
            case EmbedType.Video:
            case EmbedType.Gifv when embed.Provider is { Name: not "Tenor" }:
                return new Uri(embed.Video!.Value.Url);
            default:
                return null;
        }
    }
}

public class ImageImporter(HttpClient httpClient, AppDbContext dbContext)
{
    public async Task ImportAsync(ulong guildId, IMessageChannel channel, bool saveFiles = false)
    {
        if (saveFiles)
            Directory.CreateDirectory($"images/{channel.Id}");

        var fromId = await dbContext.Items.AsNoTracking()
            .Where(i => i.GuildId == guildId && i.ChannelId == channel.Id)
            .OrderByDescending(i => (long)i.MessageId)
            .Select(i => i.MessageId)
            .FirstOrDefaultAsync();

        var i = 0;
        await foreach (var message in channel.GetMessagesAsync(fromId, Direction.After, limit: int.MaxValue).Flatten())
        {
            if (message.Attachments.Count == 0 && message.Embeds.Count == 0)
                continue;

            foreach (var attachment in message.Attachments)
            {
                dbContext.Items.Add(new AttachmentItem
                {
                    GuildId = guildId, ChannelId = channel.Id, MessageId = message.Id, AttachmentId = attachment.Id
                });

                if (saveFiles)
                    await WriteFile(channel.Id, message.Id, attachment.Id, new Uri(attachment.Url));
            }

            var eId = 0u;
            foreach (var embed in message.Embeds)
            {
                var url = EmbedTools.GetUrl(embed);
                if (url != null)
                {
                    dbContext.Items.Add(new AttachmentItem()
                    {
                        GuildId = guildId, ChannelId = channel.Id, MessageId = message.Id, AttachmentId = eId,
                    });

                    if (saveFiles)
                        await WriteFile(channel.Id, message.Id, eId++, url);
                }
            }

            if (++i % 60 == 0)
                await dbContext.SaveChangesAsync();
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task WriteFile(ulong channelId, ulong messageId, ulong attachmentId, Uri url)
    {
        var fileName = url.Segments[^1];

        if (fileName == "hit")
        {
            if (url is { Host: "api.fxtwitter.com" })
            {
                url = new Uri(HttpUtility.ParseQueryString(url.Query)["url"]!);
                fileName = url.Segments[^1];
            }
        }

        if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
        {
            fileName += url switch
            {
                { Host: "mosaic.fxtwitter.com", Segments: ["/", "jpeg/", ..] } => ".jpg",
                _ => ".bmp"
            };
        }

        var path = $"images/{channelId}/{messageId}_{attachmentId}_{fileName}";
        if (File.Exists(path))
            return;

        await using var fStream = File.Open(path, FileMode.CreateNew, FileAccess.Write);

        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        await response.Content.CopyToAsync(fStream);
    }
}