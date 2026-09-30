using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>ItemAdd</c> message, received when a wall item is placed in the room.</summary>
/// <param name="Item">The placed wall item, with <see cref="Furni.OwnerName"/> set from the packet.</param>
public sealed record WallItemAdd(WallItem Item) : IParserComposer<WallItemAdd>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WallItemAdd Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WallItemAdd ParseFlash(in PacketReader p) => ParseItem(in p, 24);

    private static WallItemAdd ParseItem(in PacketReader p, int minimum_size)
    {
        RoomPlacementWire.RequireMinimum(in p, minimum_size, nameof(WallItemAdd));
        var item = p.Parse<WallItem>();
        item.OwnerName = p.ReadString();
        RoomPlacementWire.RequireEmpty(in p, nameof(WallItemAdd));
        return new WallItemAdd(item);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WallItemAdd value, in PacketWriter p)
    {
        RoomPlacementWire.ValidateWallItem(value.Item, true, in p);
        value.ComposeItem(in p);
    }

    private void ComposeItem(in PacketWriter p)
    {
        p.Compose(Item);
        p.WriteString(Item.OwnerName);
    }
}
