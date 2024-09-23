namespace BlastFromThePast;

public readonly struct AttachmentItem
{
    public ulong ChannelId { get; init; }
    public ulong MessageId { get; init; }
    public ulong AttachmentId { get; init; }

    public static AttachmentItem FromBase64(string s)
    {
        var bytes = Convert.FromBase64String(s);

        return new AttachmentItem()
        {
            ChannelId = BitConverter.ToUInt64(bytes, 0),
            MessageId = BitConverter.ToUInt64(bytes, 8),
            AttachmentId = BitConverter.ToUInt64(bytes, 16),
        };
    }

    public string ToBase64()
    {
        var bytes = new byte[24];

        BitConverter.TryWriteBytes(new Span<byte>(bytes, 0, 8), ChannelId);
        BitConverter.TryWriteBytes(new Span<byte>(bytes, 8, 8), MessageId);
        BitConverter.TryWriteBytes(new Span<byte>(bytes, 16, 8), AttachmentId);

        return Convert.ToBase64String(bytes);
    }
}