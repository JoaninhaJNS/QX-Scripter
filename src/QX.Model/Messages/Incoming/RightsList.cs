using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents a user ID paired with the user's name.</summary>
/// <param name="Id">The user ID.</param>
/// <param name="Name">The user's name.</param>
public readonly record struct IdName(Id Id, string Name) : IParserComposer<IdName>
{
    /// <summary>Parses the entry from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static IdName Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static IdName ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadString());

    /// <summary>Composes the entry into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(IdName value, in PacketWriter p)
    {
        p.WriteId(value.Id);
        p.WriteString(value.Name);
    }
}

/// <summary>Represents the <c>FlatControllers</c> message, received with the users who have rights in a room.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="Users">The users with rights in the room, at most 65535.</param>
public sealed record RightsList(Id RoomId, IReadOnlyList<IdName> Users) : IParserComposer<RightsList>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RightsList Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RightsList ParseFlash(in PacketReader p) =>
        ParseUsers(p.ReadInt(), checked((ushort)p.ReadInt()), in p);

    private static RightsList ParseUsers(Id room_id, int count, in PacketReader p)
    {
        var users = new IdName[count];
        for (int i = 0; i < count; i++)
            users[i] = p.Parse<IdName>();
        return new RightsList(room_id, users);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RightsList value, in PacketWriter p)
    {
        ushort count = checked((ushort)value.Users.Count);
        p.WriteId(value.RoomId);
        p.WriteInt(count);
        foreach (IdName user in value.Users)
            p.Compose(user);
    }
}
