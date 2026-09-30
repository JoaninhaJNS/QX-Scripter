using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>MiniMailUnreadCount</c> message, received with the number of unread mini mail messages.</summary>
/// <param name="Count">The number of unread mini mail messages.</param>
public sealed record MiniMailUnreadCount(int Count) : IParserComposer<MiniMailUnreadCount>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static MiniMailUnreadCount Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(Count);
}
