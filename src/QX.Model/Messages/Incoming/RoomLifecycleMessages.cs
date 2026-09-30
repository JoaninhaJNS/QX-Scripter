using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RoomReady</c> message, received when the room the user is entering is ready to load.</summary>
/// <param name="RoomType">The room type sent by the server.</param>
/// <param name="RoomId">The ID of the room.</param>
public sealed record RoomReady(string RoomType, Id RoomId) : IParserComposer<RoomReady>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomReady Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomReady ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomReady value, in PacketWriter p)
    {
        p.WriteString(value.RoomType);
        p.WriteId(value.RoomId);
    }
}

/// <summary>Represents the <c>RoomForward</c> message, received when the server sends the user to another room.</summary>
/// <param name="RoomId">The ID of the room the user is sent to.</param>
public sealed record RoomForward(Id RoomId) : IParserComposer<RoomForward>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomForward Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomForward ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomForward value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}

/// <summary>Represents the <c>CloseConnection</c> message, received when the server closes the user's connection to the room.</summary>
/// <param name="Reason">The reason code, or <see langword="null"/> when the packet carries no reason.</param>
public sealed record CloseConnection(short? Reason) : IParserComposer<CloseConnection>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CloseConnection Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CloseConnection ParseFlash(in PacketReader p) =>
        new(p.Available >= 2 ? p.ReadShort() : null);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CloseConnection value, in PacketWriter p)
    {
        if (value.Reason is short reason)
            p.WriteShort(reason);
    }
}
