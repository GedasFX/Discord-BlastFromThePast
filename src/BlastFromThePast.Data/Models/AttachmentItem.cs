namespace BlastFromThePast.Data.Models;

public class AttachmentItem
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong AttachmentId { get; set; }

    public static AttachmentItem FromBase64(string s)
    {
        var bytes = Convert.FromBase64String(s);

        return new AttachmentItem()
        {
            GuildId = BitConverter.ToUInt64(bytes, 0),
            ChannelId = BitConverter.ToUInt64(bytes, 8),
            MessageId = BitConverter.ToUInt64(bytes, 16),
            AttachmentId = BitConverter.ToUInt64(bytes, 24),
        };
    }

    public string ToBase64()
    {
        var bytes = new byte[32];

        BitConverter.TryWriteBytes(new Span<byte>(bytes, 0, 8), GuildId);
        BitConverter.TryWriteBytes(new Span<byte>(bytes, 8, 8), ChannelId);
        BitConverter.TryWriteBytes(new Span<byte>(bytes, 16, 8), MessageId);
        BitConverter.TryWriteBytes(new Span<byte>(bytes, 24, 8), AttachmentId);

        return Convert.ToBase64String(bytes);
    }
}