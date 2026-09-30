using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Doorbell</c> message, received when a doorbell rings for a room.</summary>
/// <param name="UserName">
/// The name of the user ringing the doorbell, or an empty string when the local user is the one waiting at the door.
/// </param>
public sealed record Doorbell(string UserName) : IParserComposer<Doorbell>
{

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Doorbell Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static Doorbell ParseFlash(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(Doorbell value, in PacketWriter p) =>
        p.WriteString(value.UserName);
}
