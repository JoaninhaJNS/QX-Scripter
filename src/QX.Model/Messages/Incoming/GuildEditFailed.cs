using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>GuildEditFailed</c> message, received when editing a group fails.</summary>
/// <param name="Reason">The failure reason code sent by the server.</param>
public sealed record GuildEditFailed(int Reason) : IParserComposer<GuildEditFailed>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GuildEditFailed Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(Reason);
}
