using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>NestBreedingSuccess</c> message, received when pets in a breeding nest produce a new pet.</summary>
/// <param name="PetId">The identifier of the newly bred pet.</param>
/// <param name="RarityCategory">The rarity category of the newly bred pet.</param>
public sealed record NestBreedingSuccess(Id PetId, int RarityCategory) : IParserComposer<NestBreedingSuccess>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NestBreedingSuccess Parse(in PacketReader p) => new(p.ReadId(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteId(PetId);
        p.WriteInt(RarityCategory);
    }
}
