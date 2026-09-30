using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>UserNameChanged</c> message, received when a user in the room changes their name.</summary>
/// <param name="WebId">The user ID of the user whose name changed.</param>
/// <param name="Index">The room index of the user whose name changed.</param>
/// <param name="NewName">The user's new name.</param>
public sealed record UserNameChanged(Id WebId, int Index, string NewName)
    : IParserComposer<UserNameChanged>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UserNameChanged Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UserNameChanged ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadInt(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UserNameChanged value, in PacketWriter p)
    {
        p.WriteId(value.WebId);
        p.WriteInt(value.Index);
        p.WriteString(value.NewName);
    }
}
