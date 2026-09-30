using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>MessengerInit</c> message, received when the friend list is initialized.</summary>
/// <param name="UserLimit">The user's own friend list limit.</param>
/// <param name="NormalLimit">The friend list limit for normal users.</param>
/// <param name="ExtendedLimit">The extended friend list limit.</param>
/// <param name="Categories">The user's friend categories.</param>
/// <param name="FriendCount">The number of friends, or 0 when the packet does not carry the counts.</param>
/// <param name="FriendRequestCount">The number of pending friend requests, or 0 when the packet does not carry the counts.</param>
public sealed record MessengerInit(
    int UserLimit,
    int NormalLimit,
    int ExtendedLimit,
    IReadOnlyList<FriendCategory> Categories,
    int FriendCount = 0,
    int FriendRequestCount = 0) : IParserComposer<MessengerInit>
{
    /// <summary>Gets whether the packet carried <see cref="FriendCount"/> and <see cref="FriendRequestCount"/>.</summary>
    /// <remarks>
    /// The parser reads the two counts only when at least 8 bytes remain after the categories. When
    /// <see langword="false"/>, composing leaves the counts out.
    /// </remarks>
    public bool HasCounts { get; init; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static MessengerInit Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static MessengerInit ParseFlash(in PacketReader p)
    {
        int userLimit = p.ReadInt();
        int normalLimit = p.ReadInt();
        int extendedLimit = p.ReadInt();

        int count = p.ReadLength();
        var categories = new List<FriendCategory>(count);
        for (int i = 0; i < count; i++)
            categories.Add(p.Parse<FriendCategory>());

        bool has_counts = p.Available >= 8;
        int friendCount = has_counts ? p.ReadInt() : 0;
        int friendRequestCount = has_counts ? p.ReadInt() : 0;
        return new MessengerInit(userLimit, normalLimit, extendedLimit, categories, friendCount, friendRequestCount)
        {
            HasCounts = has_counts
        };
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(MessengerInit value, in PacketWriter p)
    {
        p.WriteInt(value.UserLimit);
        p.WriteInt(value.NormalLimit);
        p.WriteInt(value.ExtendedLimit);

        p.WriteLength((Length)value.Categories.Count);
        foreach (FriendCategory category in value.Categories)
            p.Compose(category);

        if (value.HasCounts)
        {
            p.WriteInt(value.FriendCount);
            p.WriteInt(value.FriendRequestCount);
        }
    }
}
