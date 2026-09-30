using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RoomEntryInfo</c> message, received when the user has entered a room.</summary>
/// <param name="GuestRoomId">The ID of the room that was entered.</param>
/// <param name="Owner">Whether the user owns the room.</param>
public sealed record RoomEntryInfo(Id GuestRoomId, bool Owner) : IParserComposer<RoomEntryInfo>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomEntryInfo Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomEntryInfo ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadBool());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomEntryInfo value, in PacketWriter p)
    {
        p.WriteId(value.GuestRoomId);
        p.WriteBool(value.Owner);
    }
}
