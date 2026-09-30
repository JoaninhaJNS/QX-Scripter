using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RecyclerStatus</c> message, received with the state of the recycler and the time it has left.</summary>
/// <param name="Status">The recycler state code as the hotel numbers it.</param>
/// <param name="TimeoutSeconds">The time in seconds until the running recycler session ends.</param>
public sealed record RecyclerStatus(int Status, int TimeoutSeconds) : IParserComposer<RecyclerStatus>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RecyclerStatus Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RecyclerStatus ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RecyclerStatus value, in PacketWriter p)
    {
        p.WriteInt(value.Status);
        p.WriteInt(value.TimeoutSeconds);
    }
}

/// <summary>Represents the <c>RecyclerFinished</c> message, received when a recycler session ends.</summary>
/// <param name="Status">The result code as the hotel numbers it.</param>
/// <param name="PrizeId">The identifier of the prize that was won, when the session produced one.</param>
public sealed record RecyclerFinished(int Status, int PrizeId) : IParserComposer<RecyclerFinished>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RecyclerFinished Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RecyclerFinished ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RecyclerFinished value, in PacketWriter p)
    {
        p.WriteInt(value.Status);
        p.WriteInt(value.PrizeId);
    }
}
