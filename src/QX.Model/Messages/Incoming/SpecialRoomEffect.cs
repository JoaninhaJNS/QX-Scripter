using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>SpecialRoomEffect</c> message, received when the room plays a special visual effect.</summary>
/// <param name="EffectId">The identifier of the effect to play.</param>
public sealed record SpecialRoomEffect(int EffectId) : IParserComposer<SpecialRoomEffect>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SpecialRoomEffect Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(EffectId);
}
