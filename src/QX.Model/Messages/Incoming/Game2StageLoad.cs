using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Game2StageLoad</c> message, received when the client should load a game stage.</summary>
/// <param name="GameType">The type of the game being loaded.</param>
public sealed record Game2StageLoad(int GameType) : IParserComposer<Game2StageLoad>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Game2StageLoad Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(GameType);
}
