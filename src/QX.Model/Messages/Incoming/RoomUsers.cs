using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>Users</c> message, received when users, pets or bots are added to the room.</summary>
/// <param name="Avatars">The avatars that were added.</param>
public sealed record RoomUsers(IReadOnlyList<Avatar> Avatars) : IParserComposer<RoomUsers>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomUsers Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomUsers ParseFlash(in PacketReader p) => new(p.ParseArray<Avatar>());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomUsers value, in PacketWriter p) =>
        p.ComposeArray(value.Avatars);
}
