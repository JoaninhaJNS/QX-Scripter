using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>UserNftWardrobeSelection</c> message, received with the user's selected NFT wardrobe outfit.</summary>
/// <param name="CurrentTokenId">The token ID of the selected NFT outfit.</param>
/// <param name="FallbackFigureString">The figure string to use when the NFT outfit is not worn.</param>
/// <param name="FallbackFigureGender">The gender of the fallback figure.</param>
public sealed record UserNftWardrobeSelection(string CurrentTokenId, string FallbackFigureString, string FallbackFigureGender)
    : IParserComposer<UserNftWardrobeSelection>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UserNftWardrobeSelection Parse(in PacketReader p) => new(p.ReadString(), p.ReadString(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(CurrentTokenId);
        p.WriteString(FallbackFigureString);
        p.WriteString(FallbackFigureGender);
    }
}
