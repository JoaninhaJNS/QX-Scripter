using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>ClaimProductResult</c> message, received with the result of a product claim.</summary>
/// <param name="ClaimId">The identifier of the claim.</param>
/// <param name="Result">The result code sent by the server.</param>
public sealed record ClaimProductResult(string ClaimId, int Result) : IParserComposer<ClaimProductResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ClaimProductResult Parse(in PacketReader p) => new(p.ReadString(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(ClaimId);
        p.WriteInt(Result);
    }
}
