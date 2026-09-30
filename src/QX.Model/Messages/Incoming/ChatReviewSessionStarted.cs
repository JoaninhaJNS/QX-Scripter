using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>ChatReviewSessionStarted</c> message, received when a chat review session starts.
/// </summary>
/// <param name="VotingTimeout">The time the reviewer has to vote.</param>
/// <param name="ChatRecord">The chat log under review.</param>
public sealed record ChatReviewSessionStarted(int VotingTimeout, string ChatRecord)
    : IParserComposer<ChatReviewSessionStarted>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ChatReviewSessionStarted Parse(in PacketReader p) => new(p.ReadInt(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(VotingTimeout);
        p.WriteString(ChatRecord);
    }
}
