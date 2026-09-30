using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>CustomUserNotification</c> message, received when the server shows the user a predefined
/// notification.
/// </summary>
/// <param name="Code">The code of the notification.</param>
public sealed record CustomUserNotification(int Code) : IParserComposer<CustomUserNotification>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CustomUserNotification Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(Code);
}
