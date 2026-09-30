using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RoomEntryTile</c> message, received with the tile where avatars enter the room.</summary>
/// <param name="X">The x coordinate of the entry tile.</param>
/// <param name="Y">The y coordinate of the entry tile.</param>
/// <param name="Direction">The direction avatars face when they enter.</param>
public sealed record RoomEntryTile(int X, int Y, int Direction) : IParserComposer<RoomEntryTile>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomEntryTile Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomEntryTile ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomEntryTile value, in PacketWriter p)
    {
        p.WriteInt(value.X);
        p.WriteInt(value.Y);
        p.WriteInt(value.Direction);
    }
}

/// <summary>Represents the <c>RoomProperty</c> message, received when a room decoration property such as the wallpaper or floor is set.</summary>
/// <param name="Key">The property name, such as <c>wallpaper</c>, <c>floor</c> or <c>landscape</c>.</param>
/// <param name="Value">The property value.</param>
public sealed record FlatProperty(string Key, string Value) : IParserComposer<FlatProperty>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FlatProperty Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FlatProperty ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FlatProperty value, in PacketWriter p)
    {
        p.WriteString(value.Key);
        p.WriteString(value.Value);
    }
}

/// <summary>Represents the <c>RoomVisualizationSettings</c> message, received with how the room's walls and floor are drawn.</summary>
/// <param name="WallsHidden">Whether the walls are hidden.</param>
/// <param name="WallThickness">The wall thickness.</param>
/// <param name="FloorThickness">The floor thickness.</param>
public sealed record RoomVisualizationSettings(
    bool WallsHidden,
    RoomThickness WallThickness,
    RoomThickness FloorThickness) : IParserComposer<RoomVisualizationSettings>
{
    /// <summary>Gets the wall thickness as a scale factor, from 0.25 for <see cref="RoomThickness.Thinnest"/> to 2 for <see cref="RoomThickness.Thick"/>.</summary>
    /// <remarks>Computed as 2 to the power of the thickness value clamped to the range -2 to 1.</remarks>
    public float WallThicknessMultiplier => ThicknessMultiplier(WallThickness);
    /// <summary>Gets the floor thickness as a scale factor, from 0.25 for <see cref="RoomThickness.Thinnest"/> to 2 for <see cref="RoomThickness.Thick"/>.</summary>
    /// <remarks>Computed as 2 to the power of the thickness value clamped to the range -2 to 1.</remarks>
    public float FloorThicknessMultiplier => ThicknessMultiplier(FloorThickness);

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomVisualizationSettings Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomVisualizationSettings ParseFlash(in PacketReader p) =>
        new(p.ReadBool(), (RoomThickness)p.ReadInt(), (RoomThickness)p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomVisualizationSettings value, in PacketWriter p)
    {
        p.WriteBool(value.WallsHidden);
        p.WriteInt((int)value.WallThickness);
        p.WriteInt((int)value.FloorThickness);
    }

    private static float ThicknessMultiplier(RoomThickness value) =>
        MathF.Pow(2, Math.Clamp((int)value, -2, 1));
}

/// <summary>Represents the <c>YouAreController</c> message, received when the user is given rights in a room.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="RightsLevel">The user's rights level in the room.</param>
public sealed record YouAreController(Id RoomId, int RightsLevel)
    : IParserComposer<YouAreController>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static YouAreController Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static YouAreController ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(YouAreController value, in PacketWriter p)
    {
        p.WriteId(value.RoomId);
        p.WriteInt(value.RightsLevel);
    }
}

/// <summary>Represents the <c>YouAreNotController</c> message, received when the user's rights in a room are removed.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record YouAreNotController(Id RoomId) : IParserComposer<YouAreNotController>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static YouAreNotController Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static YouAreNotController ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(YouAreNotController value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}

/// <summary>Represents the <c>YouAreOwner</c> message, received when the user is the owner of the room.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record YouAreOwner(Id RoomId) : IParserComposer<YouAreOwner>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static YouAreOwner Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static YouAreOwner ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(YouAreOwner value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}

/// <summary>Represents the <c>YouAreSpectator</c> message, received when the user enters a room as a spectator.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record YouAreSpectator(Id RoomId) : IParserComposer<YouAreSpectator>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static YouAreSpectator Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static YouAreSpectator ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(YouAreSpectator value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}
