using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>InfoHotelClosing</c> message, received when the hotel announces that it is about to close.</summary>
/// <param name="MinutesUntilClosing">The number of minutes until the hotel closes.</param>
public sealed record InfoHotelClosing(int MinutesUntilClosing) : IParserComposer<InfoHotelClosing>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static InfoHotelClosing Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(MinutesUntilClosing);
}
