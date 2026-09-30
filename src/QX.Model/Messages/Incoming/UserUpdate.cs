using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>UserUpdate</c> message, received when the position, direction or status of avatars in the room changes.</summary>
/// <param name="Updates">The status updates, one per changed avatar.</param>
public sealed record UserUpdate(IReadOnlyList<AvatarStatus> Updates) : IParserComposer<UserUpdate>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UserUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UserUpdate ParseFlash(in PacketReader p) => new(p.ParseArray<AvatarStatus>());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UserUpdate value, in PacketWriter p) =>
        p.ComposeArray(value.Updates);
}
