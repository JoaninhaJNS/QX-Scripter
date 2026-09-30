using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the incoming <c>NftStorePurchase</c> message, received with the outcome of an NFT store purchase.</summary>
/// <param name="Result">The purchase result code, sent as a 16 bit integer.</param>
public sealed record NftStorePurchase(short Result) : IParserComposer<NftStorePurchase>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NftStorePurchase Parse(in PacketReader p) => new(p.ReadShort());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteShort(Result);
}
