using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>ObjectUpdate</c> message, received when a floor item in the room is updated.</summary>
/// <param name="Item">The updated floor item.</param>
public sealed record FloorItemUpdate(FloorItem Item) : IParserComposer<FloorItemUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FloorItemUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FloorItemUpdate ParseFlash(in PacketReader p) => ParseItem(in p, 44);

    private static FloorItemUpdate ParseItem(in PacketReader p, int minimum_size)
    {
        RoomPlacementWire.RequireMinimum(in p, minimum_size, nameof(FloorItemUpdate));
        var result = new FloorItemUpdate(p.Parse<FloorItem>());
        RoomPlacementWire.RequireEmpty(in p, nameof(FloorItemUpdate));
        return result;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FloorItemUpdate value, in PacketWriter p)
    {
        RoomPlacementWire.ValidateFloorItem(value.Item, false, in p);
        p.Compose(value.Item);
    }
}

/// <summary>Represents the <c>ItemUpdate</c> message, received when a wall item in the room is updated.</summary>
/// <param name="Item">The updated wall item.</param>
public sealed record WallItemUpdate(WallItem Item) : IParserComposer<WallItemUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WallItemUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WallItemUpdate ParseFlash(in PacketReader p) => ParseItem(in p, 22);

    private static WallItemUpdate ParseItem(in PacketReader p, int minimum_size)
    {
        RoomPlacementWire.RequireMinimum(in p, minimum_size, nameof(WallItemUpdate));
        var result = new WallItemUpdate(p.Parse<WallItem>());
        RoomPlacementWire.RequireEmpty(in p, nameof(WallItemUpdate));
        return result;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WallItemUpdate value, in PacketWriter p)
    {
        RoomPlacementWire.ValidateWallItem(value.Item, false, in p);
        p.Compose(value.Item);
    }
}

/// <summary>
/// Represents the <c>ObjectDataUpdate</c> message, received when the data of a floor item in the room changes.
/// </summary>
/// <param name="Id">
/// The identifier of the floor item, sent on the wire as a string and read as 0 when it is not a valid number.
/// </param>
/// <param name="Data">The new item data.</param>
public sealed record FloorItemDataUpdate(Id Id, ItemData Data) : IParserComposer<FloorItemDataUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FloorItemDataUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FloorItemDataUpdate ParseFlash(in PacketReader p)
    {
        Id id = long.TryParse(p.ReadString(), out long value) ? value : 0;
        return ParseItem(in p, id);
    }

    private static FloorItemDataUpdate ParseItem(in PacketReader p, Id id) =>
        new(id, p.Parse<ItemData>());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FloorItemDataUpdate value, in PacketWriter p)
    {
        p.WriteString(value.Id.ToString());
        p.Compose(value.Data);
    }
}

/// <summary>Represents the new data of one floor item in a <see cref="FloorItemsDataUpdate"/> message.</summary>
/// <param name="Id">The identifier of the floor item.</param>
/// <param name="Data">The new item data.</param>
public readonly record struct FloorDataEntry(Id Id, ItemData Data);

/// <summary>
/// Represents the <c>ObjectsDataUpdate</c> message, received when the data of several floor items changes at once.
/// </summary>
/// <param name="Items">The floor items and their new data.</param>
public sealed record FloorItemsDataUpdate(IReadOnlyList<FloorDataEntry> Items) : IParserComposer<FloorItemsDataUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FloorItemsDataUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FloorItemsDataUpdate ParseFlash(in PacketReader p) => ParseItems(in p);

    private static FloorItemsDataUpdate ParseItems(in PacketReader p)
    {
        int count = p.ReadLength();
        var items = new FloorDataEntry[count];
        for (int i = 0; i < count; i++)
            items[i] = new FloorDataEntry(p.ReadId(), p.Parse<ItemData>());
        return new FloorItemsDataUpdate(items);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FloorItemsDataUpdate value, in PacketWriter p) =>
        value.ComposeItems(in p);

    private void ComposeItems(in PacketWriter p)
    {
        p.WriteLength((Length)Items.Count);
        foreach (FloorDataEntry item in Items)
        {
            p.WriteId(item.Id);
            p.Compose(item.Data);
        }
    }
}
