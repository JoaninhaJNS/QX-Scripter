using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>PurchaseNotAllowed</c> message, received when the server does not allow the user to make a catalog purchase.</summary>
/// <param name="ErrorCode">The error code the server sent for the refused purchase.</param>
public sealed record PurchaseNotAllowed(int ErrorCode) : IParserComposer<PurchaseNotAllowed>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PurchaseNotAllowed Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PurchaseNotAllowed ParseFlash(in PacketReader p) => ParseResult(in p);

    private static PurchaseNotAllowed ParseResult(in PacketReader p)
    {
        var value = new PurchaseNotAllowed(p.ReadInt());
        CatalogWire.RequireEmpty(in p, nameof(PurchaseNotAllowed));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PurchaseNotAllowed value, in PacketWriter p) =>
        p.WriteInt(value.ErrorCode);
}
