using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>CollectibleMintableItemResult</c> message, received with the result of minting a collectible item.
/// </summary>
/// <param name="MintResult">The result code sent by the server, read as a 16 bit value.</param>
public sealed record CollectibleMintableItemResult(short MintResult) : IParserComposer<CollectibleMintableItemResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CollectibleMintableItemResult Parse(in PacketReader p) => new(p.ReadShort());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteShort(MintResult);
}
