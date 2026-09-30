using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>GuildMembershipRejected</c> message, received when a request to join a group is rejected.
/// </summary>
/// <param name="GuildId">The identifier of the group.</param>
/// <param name="UserId">The identifier of the user whose request was rejected.</param>
public sealed record GuildMembershipRejected(Id GuildId, Id UserId) : IParserComposer<GuildMembershipRejected>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GuildMembershipRejected Parse(in PacketReader p) => new(p.ReadId(), p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteId(GuildId);
        p.WriteId(UserId);
    }
}
