using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>NoobnessLevel</c> message, received with how new the user is to the hotel.</summary>
/// <param name="Level">The user's noobness level as sent by the hotel.</param>
public sealed record NoobnessLevel(int Level) : IParserComposer<NoobnessLevel>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NoobnessLevel Parse(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(Level);
    }
}
