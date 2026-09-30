using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>GroupDetailsChanged</c> message, received when the details of a group change.</summary>
/// <param name="GroupId">The identifier of the group whose details changed.</param>
public sealed record GroupDetailsChanged(Id GroupId) : IParserComposer<GroupDetailsChanged>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GroupDetailsChanged Parse(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteId(GroupId);
}
