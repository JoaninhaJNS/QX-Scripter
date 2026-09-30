using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Game2UserLeftGame</c> message, received when a user leaves a game.</summary>
/// <param name="UserId">The identifier of the user who left.</param>
public sealed record Game2UserLeftGame(Id UserId) : IParserComposer<Game2UserLeftGame>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Game2UserLeftGame Parse(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteId(UserId);
}
