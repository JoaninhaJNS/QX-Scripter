using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>GetGuestRoomResult</c> message, received with the data of a room.</summary>
/// <param name="EnterRoom">Whether the result was requested for entering the room.</param>
/// <param name="Data">The room data.</param>
public sealed record GuestRoomResult(bool EnterRoom, RoomData Data) : IParserComposer<GuestRoomResult>
{
    /// <summary>
    /// Gets the room details that follow the room data, or <see langword="null"/> when they
    /// are not set.
    /// </summary>
    /// <remarks>
    /// Parsing always reads the details. Composing writes default details when this is <see langword="null"/>.
    /// </remarks>
    public RoomResultDetails? Details { get; init; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GuestRoomResult Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GuestRoomResult ParseFlash(in PacketReader p)
    {
        return new GuestRoomResult(p.ReadBool(), p.Parse<RoomData>())
        {
            Details = p.Parse<RoomResultDetails>()
        };
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GuestRoomResult value, in PacketWriter p)
    {
        p.WriteBool(value.EnterRoom);
        p.Compose(value.Data);
        p.Compose(value.Details ?? new RoomResultDetails { OpeningConnection = false });
    }
}
