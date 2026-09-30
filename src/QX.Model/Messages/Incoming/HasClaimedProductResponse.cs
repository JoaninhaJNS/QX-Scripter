using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>HasClaimedProductResponse</c> message, received in answer to a <c>HasClaimedProduct</c> request.</summary>
/// <param name="ClaimId">The identifier of the claimable product that was checked.</param>
/// <param name="HasClaimed">Whether the user has already claimed the product.</param>
public sealed record HasClaimedProductResponse(string ClaimId, bool HasClaimed)
    : IParserComposer<HasClaimedProductResponse>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HasClaimedProductResponse Parse(in PacketReader p) => new(p.ReadString(), p.ReadBool());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(ClaimId);
        p.WriteBool(HasClaimed);
    }
}
