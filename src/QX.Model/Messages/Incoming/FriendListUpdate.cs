using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Specifies the kind of change in a friend list update.</summary>
public enum FriendUpdateKind
{
    /// <summary>A friend removed from the friend list.</summary>
    Removed = -1,
    /// <summary>A friend whose details changed.</summary>
    Updated = 0,
    /// <summary>A friend added to the friend list.</summary>
    Added = 1
}

/// <summary>Represents a single change in a friend list update.</summary>
/// <param name="Kind">The kind of change.</param>
/// <param name="RemovedId">
/// The identifier of the removed friend when <paramref name="Kind"/> is <see cref="FriendUpdateKind.Removed"/>,
/// otherwise -1.
/// </param>
/// <param name="Friend">
/// The added or updated friend, or <see langword="null"/> when <paramref name="Kind"/> is
/// <see cref="FriendUpdateKind.Removed"/>.
/// </param>
public sealed record FriendUpdateEntry(FriendUpdateKind Kind, Id RemovedId, Friend? Friend);

/// <summary>
/// Represents the <c>FriendListUpdate</c> message, received when friends are added, updated or removed.
/// </summary>
/// <param name="Categories">The friend categories, which replace the current categories.</param>
/// <param name="Updates">The changes to the friend list, in the order they were sent.</param>
public sealed record FriendListUpdate(
    IReadOnlyList<FriendCategory> Categories,
    IReadOnlyList<FriendUpdateEntry> Updates) : IParserComposer<FriendListUpdate>
{
    /// <summary>Gets the friends that were added.</summary>
    public IEnumerable<Friend> Added => Entries(FriendUpdateKind.Added);
    /// <summary>Gets the friends that were updated.</summary>
    public IEnumerable<Friend> Updated => Entries(FriendUpdateKind.Updated);
    /// <summary>Gets the identifiers of the friends that were removed.</summary>
    public IEnumerable<long> Removed => Updates.Where(u => u.Kind == FriendUpdateKind.Removed).Select(u => (long)u.RemovedId);

    private IEnumerable<Friend> Entries(FriendUpdateKind kind) =>
        Updates.Where(u => u.Kind == kind && u.Friend is not null).Select(u => u.Friend!);

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendListUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FriendListUpdate ParseFlash(in PacketReader p)
    {
        int categoryCount = p.ReadLength();
        var categories = new List<FriendCategory>(categoryCount);
        for (int i = 0; i < categoryCount; i++)
            categories.Add(p.Parse<FriendCategory>());

        int updateCount = p.ReadLength();
        var updates = new List<FriendUpdateEntry>(updateCount);
        for (int i = 0; i < updateCount; i++)
        {
            FriendUpdateKind kind = (FriendUpdateKind)p.ReadInt();
            updates.Add(kind switch
            {
                FriendUpdateKind.Removed =>
                    new FriendUpdateEntry(FriendUpdateKind.Removed, p.ReadId(), null),
                FriendUpdateKind.Updated or FriendUpdateKind.Added =>
                    new FriendUpdateEntry(kind, -1, p.Parse<Friend>()),
                _ => throw new InvalidDataException($"Unknown friend-list update type {(int)kind}.")
            });
        }

        return new FriendListUpdate(categories, updates);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    /// <exception cref="InvalidDataException">
    /// Thrown when an entry has an unknown kind, a removed entry carries a friend, or an added or updated entry has no
    /// friend.
    /// </exception>
    public void Compose(in PacketWriter p)
    {
        foreach (FriendUpdateEntry entry in Updates)
            Validate(entry);

        FlashWire.Compose(this, in p, ComposeFlash);
    }

    private static void ComposeFlash(FriendListUpdate value, in PacketWriter p)
    {
        p.WriteLength((Length)value.Categories.Count);
        foreach (FriendCategory category in value.Categories)
            p.Compose(category);

        p.WriteLength((Length)value.Updates.Count);
        foreach (FriendUpdateEntry entry in value.Updates)
        {
            p.WriteInt((int)entry.Kind);
            if (entry.Kind == FriendUpdateKind.Removed)
                p.WriteId(entry.RemovedId);
            else
                p.Compose(entry.Friend!);
        }
    }

    private static void Validate(FriendUpdateEntry entry)
    {
        if (entry.Kind is not (FriendUpdateKind.Removed or FriendUpdateKind.Updated or FriendUpdateKind.Added))
            throw new InvalidDataException($"Unknown friend-list update type {(int)entry.Kind}.");
        if (entry.Kind is FriendUpdateKind.Removed && entry.Friend is not null)
            throw new InvalidDataException("A removed friend-list entry cannot contain a friend.");
        if (entry.Kind is not FriendUpdateKind.Removed && entry.Friend is null)
            throw new InvalidDataException("An added or updated friend-list entry must contain a friend.");
    }
}
