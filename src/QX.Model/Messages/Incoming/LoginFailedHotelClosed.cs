using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>LoginFailedHotelClosed</c> message, received when a login is refused because the hotel is closed.</summary>
/// <param name="OpenHour">The hour at which the hotel opens again.</param>
/// <param name="OpenMinute">The minute at which the hotel opens again.</param>
public sealed record LoginFailedHotelClosed(int OpenHour, int OpenMinute) : IParserComposer<LoginFailedHotelClosed>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static LoginFailedHotelClosed Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(OpenHour);
        p.WriteInt(OpenMinute);
    }
}
