using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>ItemStateUpdate</c> message, received when the data of a wall item changes.</summary>
/// <remarks>Also used as one entry of <see cref="WallItemsStateUpdate"/>.</remarks>
/// <param name="Id">The ID of the wall item.</param>
/// <param name="ItemData">The wall item's new data string.</param>
public sealed record ItemStateUpdate(Id Id, string ItemData) : IParserComposer<ItemStateUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ItemStateUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ItemStateUpdate ParseFlash(in PacketReader p) => ParseItem(in p);

    private static ItemStateUpdate ParseItem(in PacketReader p) => new(p.ReadId(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ItemStateUpdate value, in PacketWriter p) => value.ComposeItem(in p);

    private void ComposeItem(in PacketWriter p)
    {
        p.WriteId(Id);
        p.WriteString(ItemData);
    }
}

/// <summary>Represents the <c>ItemsStateUpdate</c> message, received when the data of several wall items changes at once.</summary>
/// <param name="Items">The wall items and their new data.</param>
public sealed record WallItemsStateUpdate(IReadOnlyList<ItemStateUpdate> Items)
    : IParserComposer<WallItemsStateUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WallItemsStateUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WallItemsStateUpdate ParseFlash(in PacketReader p)
    {
        int count = p.ReadLength();
        var items = new ItemStateUpdate[count];
        for (int i = 0; i < count; i++)
            items[i] = p.Parse<ItemStateUpdate>();
        return new WallItemsStateUpdate(items);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WallItemsStateUpdate value, in PacketWriter p)
    {
        p.WriteLength((Length)value.Items.Count);
        foreach (ItemStateUpdate item in value.Items)
            p.Compose(item);
    }
}
