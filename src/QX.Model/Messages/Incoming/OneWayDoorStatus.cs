using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>OneWayDoorStatus</c> message, received when the state of a one way door furni changes.</summary>
/// <param name="ItemId">The floor item whose state changed.</param>
/// <param name="Status">
/// The new state. The client passes this straight into the furni's state slot and replaces the
/// furni's stuff data with an empty one.
/// </param>
public sealed record OneWayDoorStatus(Id ItemId, int Status) : IParserComposer<OneWayDoorStatus>
{
    /// <summary>Gets the optional trailing integer some Flash builds append, or <see langword="null"/> when the packet ends after the status.</summary>
    public int? FlashTrailingValue { get; init; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static OneWayDoorStatus Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static OneWayDoorStatus ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadInt())
        {
            FlashTrailingValue = p.Available switch
            {
                0 => null,
                4 => p.ReadInt(),
                _ => throw new InvalidDataException("Flash one-way door status requires either no trailing data or one trailing integer.")
            }
        };

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(OneWayDoorStatus value, in PacketWriter p)
    {
        p.WriteId(value.ItemId);
        p.WriteInt(value.Status);
        if (value.FlashTrailingValue is int trailing_value)
            p.WriteInt(trailing_value);
    }
}
