using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>GenericError</c> message, received when the server reports an error.</summary>
/// <param name="ErrorCode">
/// The error code sent by the server, for example 4008 when the room owner kicked the user or -100002 for a wrong room
/// password.
/// </param>
public sealed record GenericError(int ErrorCode) : IParserComposer<GenericError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GenericError Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GenericError ParseFlash(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GenericError value, in PacketWriter p) =>
        p.WriteInt(value.ErrorCode);
}
