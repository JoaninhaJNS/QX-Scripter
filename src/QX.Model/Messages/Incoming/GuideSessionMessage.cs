using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>GuideSessionMessage</c> message, received when a chat message is sent in a guide session.
/// </summary>
/// <param name="ChatMessage">The text of the chat message.</param>
/// <param name="SenderId">The identifier of the user who sent the message.</param>
public sealed record GuideSessionMessage(string ChatMessage, Id SenderId) : IParserComposer<GuideSessionMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GuideSessionMessage Parse(in PacketReader p) => new(p.ReadString(), p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(ChatMessage);
        p.WriteId(SenderId);
    }
}
