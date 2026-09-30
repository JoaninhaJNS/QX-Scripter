using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>FriendFurniCancelLock</c> message, received when the lock on a friend furni item is canceled.
/// </summary>
/// <param name="StuffId">The identifier of the furni item.</param>
public sealed record FriendFurniCancelLock(Id StuffId) : IParserComposer<FriendFurniCancelLock>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendFurniCancelLock Parse(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteId(StuffId);
}
