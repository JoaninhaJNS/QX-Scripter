using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>FriendsListFragment</c> message, received with one fragment of the friend list.
/// </summary>
/// <param name="Total">The total number of fragments in the friend list.</param>
/// <param name="Index">The zero based index of this fragment.</param>
/// <param name="Friends">The friends in this fragment.</param>
public sealed record FriendListFragment(int Total, int Index, IReadOnlyList<Friend> Friends) : IParserComposer<FriendListFragment>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendListFragment Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FriendListFragment ParseFlash(in PacketReader p)
    {
        int total = p.ReadInt();
        int index = p.ReadInt();

        int count = p.ReadLength();
        var friends = new List<Friend>(count);
        for (int i = 0; i < count; i++)
            friends.Add(p.Parse<Friend>());

        return new FriendListFragment(total, index, friends);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FriendListFragment value, in PacketWriter p)
    {
        p.WriteInt(value.Total);
        p.WriteInt(value.Index);

        p.WriteLength((Length)value.Friends.Count);
        foreach (Friend friend in value.Friends)
            p.Compose(friend);
    }
}
