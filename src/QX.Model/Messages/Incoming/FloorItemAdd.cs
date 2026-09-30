using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>ObjectAdd</c> message, received when a floor item is added to the room.</summary>
/// <param name="Item">The added floor item, with its owner name read from the end of the message.</param>
public sealed record FloorItemAdd(FloorItem Item) : IParserComposer<FloorItemAdd>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FloorItemAdd Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FloorItemAdd ParseFlash(in PacketReader p) => ParseItem(in p, 46);

    private static FloorItemAdd ParseItem(in PacketReader p, int minimum_size)
    {
        RoomPlacementWire.RequireMinimum(in p, minimum_size, nameof(FloorItemAdd));
        var item = p.Parse<FloorItem>();
        item.OwnerName = p.ReadString();
        RoomPlacementWire.RequireEmpty(in p, nameof(FloorItemAdd));
        return new FloorItemAdd(item);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FloorItemAdd value, in PacketWriter p)
    {
        RoomPlacementWire.ValidateFloorItem(value.Item, true, in p);
        value.ComposeItem(in p);
    }

    private void ComposeItem(in PacketWriter p)
    {
        p.Compose(Item);
        p.WriteString(Item.OwnerName);
    }
}
