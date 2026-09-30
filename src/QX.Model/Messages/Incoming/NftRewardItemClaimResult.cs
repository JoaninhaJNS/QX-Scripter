using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>NftRewardItemClaimResult</c> message, received with the outcome of claiming an NFT reward item.</summary>
/// <param name="CollectionId">The identifier of the NFT collection the reward belongs to.</param>
/// <param name="WalletAddress">The wallet address the reward was claimed to.</param>
/// <param name="Success">Whether the claim succeeded.</param>
public sealed record NftRewardItemClaimResult(string CollectionId, string WalletAddress, bool Success)
    : IParserComposer<NftRewardItemClaimResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NftRewardItemClaimResult Parse(in PacketReader p) => new(p.ReadString(), p.ReadString(), p.ReadBool());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(CollectionId);
        p.WriteString(WalletAddress);
        p.WriteBool(Success);
    }
}
