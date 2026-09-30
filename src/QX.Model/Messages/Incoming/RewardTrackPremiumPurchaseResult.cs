using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RewardTrackPremiumPurchaseResult</c> message, received with the outcome of buying the premium tier of a reward track.</summary>
/// <param name="TrackId">The identifier of the reward track.</param>
/// <param name="ResultCode">The result code of the purchase.</param>
/// <param name="Points">The reward track points value sent with the result.</param>
public sealed record RewardTrackPremiumPurchaseResult(string TrackId, int ResultCode, int Points)
    : IParserComposer<RewardTrackPremiumPurchaseResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RewardTrackPremiumPurchaseResult Parse(in PacketReader p) =>
        new(p.ReadString(), p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(TrackId);
        p.WriteInt(ResultCode);
        p.WriteInt(Points);
    }
}
