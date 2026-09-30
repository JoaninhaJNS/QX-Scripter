using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>PurchaseError</c> message, received when the server rejects a catalog purchase.</summary>
/// <param name="ErrorCode">The error code the server sent for the failed purchase.</param>
public sealed record PurchaseError(int ErrorCode) : IParserComposer<PurchaseError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PurchaseError Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PurchaseError ParseFlash(in PacketReader p) => ParseResult(in p);

    private static PurchaseError ParseResult(in PacketReader p)
    {
        var value = new PurchaseError(p.ReadInt());
        CatalogWire.RequireEmpty(in p, nameof(PurchaseError));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PurchaseError value, in PacketWriter p) =>
        p.WriteInt(value.ErrorCode);
}
