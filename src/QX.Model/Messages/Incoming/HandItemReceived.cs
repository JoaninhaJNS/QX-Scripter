using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>HandItemReceived</c> message, received when another user passes a hand item to the user.</summary>
/// <param name="GiverId">The room index of the user who passed the item, not their user ID.</param>
/// <param name="HandItemType">The type of the hand item that was received.</param>
public sealed record HandItemReceived(Id GiverId, int HandItemType) : IParserComposer<HandItemReceived>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HandItemReceived Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HandItemReceived ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HandItemReceived value, in PacketWriter p)
    {
        p.WriteInt(checked((int)value.GiverId));
        p.WriteInt(value.HandItemType);
    }
}
