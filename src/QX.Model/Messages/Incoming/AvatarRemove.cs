using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>UserRemove</c> message, received when an avatar leaves the room.</summary>
/// <param name="Index">The room index of the removed avatar, sent on the wire as a decimal string.</param>
public sealed record AvatarRemove(int Index) : IParserComposer<AvatarRemove>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AvatarRemove Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AvatarRemove ParseFlash(in PacketReader p) =>
        new(int.Parse(p.ReadString(), System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AvatarRemove value, in PacketWriter p) =>
        p.WriteString(value.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
