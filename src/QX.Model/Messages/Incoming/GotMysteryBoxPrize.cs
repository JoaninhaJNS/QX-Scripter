using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>GotMysteryBoxPrize</c> message, received when a mystery box gives a prize.</summary>
/// <param name="ContentType">The content type of the prize.</param>
/// <param name="ClassId">The class identifier of the prize.</param>
public sealed record GotMysteryBoxPrize(string ContentType, int ClassId) : IParserComposer<GotMysteryBoxPrize>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GotMysteryBoxPrize Parse(in PacketReader p) => new(p.ReadString(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(ContentType);
        p.WriteInt(ClassId);
    }
}
