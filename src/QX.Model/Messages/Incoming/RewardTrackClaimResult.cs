using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RewardTrackClaimResult</c> message, received with the outcome of claiming a reward track prize.</summary>
/// <param name="TrackId">The identifier of the reward track.</param>
/// <param name="RewardId">The identifier of the claimed reward.</param>
/// <param name="ResultCode">The result code of the claim.</param>
public sealed record RewardTrackClaimResult(string TrackId, string RewardId, int ResultCode)
    : IParserComposer<RewardTrackClaimResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RewardTrackClaimResult Parse(in PacketReader p) => new(p.ReadString(), p.ReadString(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(TrackId);
        p.WriteString(RewardId);
        p.WriteInt(ResultCode);
    }
}
