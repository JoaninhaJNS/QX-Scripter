using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Game2JoiningGameFailed</c> message, received when joining a game fails.</summary>
/// <param name="Reason">The failure reason code sent by the server.</param>
public sealed record Game2JoiningGameFailed(int Reason) : IParserComposer<Game2JoiningGameFailed>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Game2JoiningGameFailed Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(Reason);
}
