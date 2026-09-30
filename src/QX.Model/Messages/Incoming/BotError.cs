using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>BotError</c> message, received when the server rejects a bot action.</summary>
/// <param name="ErrorCode">The error code sent by the server.</param>
public sealed record BotError(int ErrorCode) : IParserComposer<BotError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static BotError Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(ErrorCode);
}
