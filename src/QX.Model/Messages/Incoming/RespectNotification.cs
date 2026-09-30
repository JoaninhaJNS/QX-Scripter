using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RespectNotification</c> message, received when a user in the room is given respect.</summary>
/// <param name="RespectedUserId">The user ID of the user who received the respect.</param>
/// <param name="TotalRespect">The total respect the user has received, as reported by the server.</param>
public sealed record RespectNotification(Id RespectedUserId, int TotalRespect)
    : IParserComposer<RespectNotification>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RespectNotification Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RespectNotification ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RespectNotification value, in PacketWriter p)
    {
        p.WriteId(value.RespectedUserId);
        p.WriteInt(value.TotalRespect);
    }
}
