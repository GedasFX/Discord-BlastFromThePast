using System.Web;
using Discord;
using Discord.WebSocket;

namespace Discord_AOT;

public class ImageImporter(SocketTextChannel channel) : IDisposable
{
    public HttpClient HttpClient { get; } = new();

    public async Task ImportAsync()
    {
        Directory.CreateDirectory($"images/{channel.Id}");

        var f = Directory.EnumerateFiles($"images/{channel.Id}").LastOrDefault();
        var fromId = !string.IsNullOrEmpty(f) ? ulong.Parse(new FileInfo(f).Name.Split('_')[0]) : 0;
        
        await foreach (var message in channel.GetMessagesAsync(fromId, Direction.After, limit: 100000).Flatten())
        {
            if (message.Attachments.Count == 0 && message.Embeds.Count == 0)
                continue;

            foreach (var attachment in message.Attachments)
            {
                await WriteFile(message.Id, attachment.Id, new Uri(attachment.Url));
            }

            var eId = 0u;
            foreach (var embed in message.Embeds)
            {
                switch (embed.Type)
                {
                    case EmbedType.Image:
                    case EmbedType.Video when embed.Provider is { Name: "YouTube" }:
                    case EmbedType.Link when embed.Thumbnail.HasValue:
                    case EmbedType.Article when embed.Thumbnail.HasValue:
                        await WriteFile(message.Id, eId++, new Uri(embed.Thumbnail!.Value.Url));
                        break;
                    case EmbedType.Article when embed.Image.HasValue:
                        await WriteFile(message.Id, eId++, new Uri(embed.Image.Value.Url));
                        break;
                    case EmbedType.Video:
                    case EmbedType.Gifv when embed.Provider is { Name: not "Tenor" }:
                        await WriteFile(message.Id, eId++, new Uri(embed.Video!.Value.Url));
                        break;
                }
            }
        }
    }

    private async Task WriteFile(ulong messageId, ulong attachmentId, Uri url)
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
                { Host: "mosaic.fxtwitter.com", Segments: ["/", "jpeg/", ..]  } => ".jpg",
                _ => ".bmp"
            };
        }
        
        var path = $"images/{channel.Id}/{messageId}_{attachmentId}_{fileName}";
        if (File.Exists(path))
            return;

        await using var fStream = File.Open(path, FileMode.CreateNew, FileAccess.Write);

        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        await response.Content.CopyToAsync(fStream);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpClient.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}