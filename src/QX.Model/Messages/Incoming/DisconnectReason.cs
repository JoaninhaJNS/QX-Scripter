using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>DisconnectReason</c> message, received when the server announces that it is closing the
/// connection.
/// </summary>
/// <param name="Reason">The raw disconnect reason code sent by the server.</param>
public sealed record DisconnectReason(int Reason) : IParserComposer<DisconnectReason>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DisconnectReason Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DisconnectReason ParseFlash(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DisconnectReason value, in PacketWriter p) =>
        p.WriteInt(value.Reason);
}
