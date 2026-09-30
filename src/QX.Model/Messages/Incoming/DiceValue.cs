using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>DiceValue</c> message, received when a dice in the room changes its value.</summary>
/// <param name="ItemId">The identifier of the dice floor item.</param>
/// <param name="Value">The new value, applied as the item's state.</param>
public sealed record DiceValue(Id ItemId, int Value) : IParserComposer<DiceValue>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DiceValue Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DiceValue ParseFlash(in PacketReader p) => new(p.ReadId(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DiceValue value, in PacketWriter p)
    {
        p.WriteId(value.ItemId);
        p.WriteInt(value.Value);
    }
}
