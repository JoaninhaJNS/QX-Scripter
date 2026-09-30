using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>YouAreNotSpectator</c> message, received when the user stops spectating a room.</summary>
/// <param name="FlatId">The ID of the room the user is no longer spectating.</param>
public sealed record YouAreNotSpectator(Id FlatId) : IParserComposer<YouAreNotSpectator>
{
    /// <summary>Gets the ID of the room, the same value as <see cref="FlatId"/>.</summary>
    public Id RoomId => FlatId;

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static YouAreNotSpectator Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static YouAreNotSpectator ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(YouAreNotSpectator value, in PacketWriter p) =>
        p.WriteId(value.FlatId);
}
