using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>HabboBroadcast</c> message, received when the hotel broadcasts an alert to users.
/// </summary>
/// <param name="MessageText">The text of the broadcast.</param>
public sealed record HabboBroadcast(string MessageText) : IParserComposer<HabboBroadcast>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabboBroadcast Parse(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteString(MessageText);
}
