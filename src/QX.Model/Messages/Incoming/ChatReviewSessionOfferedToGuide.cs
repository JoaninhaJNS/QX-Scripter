using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>ChatReviewSessionOfferedToGuide</c> message, received when a chat review session is offered to the
/// user as a guide.
/// </summary>
/// <param name="AcceptanceTimeout">The time the guide has to accept the offer.</param>
public sealed record ChatReviewSessionOfferedToGuide(int AcceptanceTimeout)
    : IParserComposer<ChatReviewSessionOfferedToGuide>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ChatReviewSessionOfferedToGuide Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(AcceptanceTimeout);
}
