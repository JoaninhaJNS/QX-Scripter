using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>PetRespectFailed</c> message, received when the user's account is too young to respect a pet.</summary>
/// <param name="RequiredDays">The account age in days required to respect pets.</param>
/// <param name="AvatarAgeInDays">The user's current account age in days.</param>
public sealed record PetRespectFailed(int RequiredDays, int AvatarAgeInDays) : IParserComposer<PetRespectFailed>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PetRespectFailed Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(RequiredDays);
        p.WriteInt(AvatarAgeInDays);
    }
}
