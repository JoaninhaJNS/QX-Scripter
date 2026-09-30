using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Items</c> message, received with the wall items in the room.</summary>
/// <remarks>
/// The packet carries a table of owner IDs and names before the items. The parser fills
/// <see cref="Furni.OwnerName"/> from that table, and composing rebuilds it from the items.
/// </remarks>
/// <param name="Items">The wall items.</param>
public sealed record WallItems(IReadOnlyList<WallItem> Items) : IParserComposer<WallItems>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WallItems Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WallItems ParseFlash(in PacketReader p) => ParseItems(in p);

    private static WallItems ParseItems(in PacketReader p)
    {
        var owners = new Dictionary<long, string>();
        int count = p.ReadLength();
        for (int i = 0; i < count; i++)
            owners[p.ReadId()] = p.ReadString();

        count = p.ReadLength();
        var items = new List<WallItem>(count);
        for (int i = 0; i < count; i++)
        {
            var item = p.Parse<WallItem>();
            if (owners.TryGetValue(item.OwnerId, out string? name))
                item.OwnerName = name;
            items.Add(item);
        }

        return new WallItems(items);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WallItems value, in PacketWriter p) => value.ComposeItems(in p);

    private void ComposeItems(in PacketWriter p)
    {
        var owners = new Dictionary<long, string>();
        foreach (WallItem item in Items)
            owners.TryAdd(item.OwnerId, item.OwnerName);

        p.WriteLength((Length)owners.Count);
        foreach ((long id, string name) in owners)
        {
            p.WriteId(id);
            p.WriteString(name);
        }

        p.WriteLength((Length)Items.Count);
        foreach (WallItem item in Items)
            p.Compose(item);
    }
}
