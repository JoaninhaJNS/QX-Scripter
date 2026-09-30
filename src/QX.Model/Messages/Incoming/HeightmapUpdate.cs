using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents a change to one tile of the room heightmap.</summary>
/// <param name="X">The tile x coordinate, sent as a single byte.</param>
/// <param name="Y">The tile y coordinate, sent as a single byte.</param>
/// <param name="Value">
/// The new raw tile value, encoded like <see cref="HeightmapTile.Value"/>: negative for no floor, bit
/// <c>0x4000</c> set when the tile is blocked, and the low 14 bits the stack height in 1/256 tile units.
/// </param>
public readonly record struct HeightmapDiff(int X, int Y, short Value);

/// <summary>Represents the <c>HeightMapUpdate</c> message, received when tiles of the room heightmap change.</summary>
/// <param name="Updates">The changed tiles, at most 255 since the count is sent as a single byte.</param>
public sealed record HeightmapUpdate(IReadOnlyList<HeightmapDiff> Updates) : IParserComposer<HeightmapUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HeightmapUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HeightmapUpdate ParseFlash(in PacketReader p) => ParseUpdates(in p);

    private static HeightmapUpdate ParseUpdates(in PacketReader p)
    {
        int count = p.ReadByte();
        var updates = new HeightmapDiff[count];
        for (int i = 0; i < count; i++)
            updates[i] = new HeightmapDiff(p.ReadByte(), p.ReadByte(), p.ReadShort());
        return new HeightmapUpdate(updates);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HeightmapUpdate value, in PacketWriter p) =>
        value.ComposeUpdates(in p);

    private void ComposeUpdates(in PacketWriter p)
    {
        p.WriteByte((byte)Updates.Count);
        foreach (HeightmapDiff update in Updates)
        {
            p.WriteByte((byte)update.X);
            p.WriteByte((byte)update.Y);
            p.WriteShort(update.Value);
        }
    }
}
