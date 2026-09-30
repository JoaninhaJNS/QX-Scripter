using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>Game2AccountGameStatus</c> message, received with the user's play status for a game type.
/// </summary>
/// <param name="GameTypeId">The identifier of the game type.</param>
/// <param name="FreeGamesLeft">The number of free games the user has left.</param>
/// <param name="GamesPlayedTotal">The total number of games the user has played.</param>
public sealed record Game2AccountGameStatus(int GameTypeId, int FreeGamesLeft, int GamesPlayedTotal)
    : IParserComposer<Game2AccountGameStatus>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Game2AccountGameStatus Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(GameTypeId);
        p.WriteInt(FreeGamesLeft);
        p.WriteInt(GamesPlayedTotal);
    }
}
