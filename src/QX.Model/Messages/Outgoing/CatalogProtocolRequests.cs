using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the page tree of a catalog.</summary>
/// <remarks>Sent as the Flash <c>GetCatalogIndex</c> message.</remarks>
/// <param name="CatalogType">The catalog to load, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>.</param>
public sealed record CatalogIndexRequest(string CatalogType)
    : IParserComposer<CatalogIndexRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CatalogIndexRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CatalogIndexRequest ParseFlash(in PacketReader p) => ParseRequest(in p);

    private static CatalogIndexRequest ParseRequest(in PacketReader p)
    {
        var value = new CatalogIndexRequest(p.ReadString());
        CatalogRequestWire.RequireEmpty(in p, nameof(CatalogIndexRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CatalogIndexRequest value, in PacketWriter p) =>
        ComposeRequest(value, in p);

    private static void ComposeRequest(CatalogIndexRequest value, in PacketWriter p)
    {
        CatalogRequestWire.RequireString(value.CatalogType, nameof(CatalogType), in p);
        p.WriteString(value.CatalogType);
    }
}

/// <summary>Requests the contents of a catalog page.</summary>
/// <remarks>Sent as the Flash <c>GetCatalogPage</c> message.</remarks>
/// <param name="PageId">The id of the catalog page.</param>
/// <param name="OfferId">The offer to select on the page, or -1 for none.</param>
/// <param name="CatalogType">The catalog the page belongs to, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>.</param>
public sealed record CatalogPageRequest(
    int PageId,
    int OfferId,
    string CatalogType) : IParserComposer<CatalogPageRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CatalogPageRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CatalogPageRequest ParseFlash(in PacketReader p) => ParseRequest(in p);

    private static CatalogPageRequest ParseRequest(in PacketReader p)
    {
        var value = new CatalogPageRequest(p.ReadInt(), p.ReadInt(), p.ReadString());
        CatalogRequestWire.RequireEmpty(in p, nameof(CatalogPageRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CatalogPageRequest value, in PacketWriter p) =>
        ComposeRequest(value, in p);

    private static void ComposeRequest(CatalogPageRequest value, in PacketWriter p)
    {
        CatalogRequestWire.RequireString(value.CatalogType, nameof(CatalogType), in p);
        p.WriteInt(value.PageId);
        p.WriteInt(value.OfferId);
        p.WriteString(value.CatalogType);
    }
}

internal static class CatalogRequestWire
{
    public static void RequireEmpty(in PacketReader p, string message_name)
    {
        if (p.Available != 0)
            throw new InvalidDataException($"{message_name} contains {p.Available} unexpected bytes.");
    }

    public static void RequireString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (p.Encoding.GetByteCount(value) > ushort.MaxValue)
            throw new InvalidDataException($"{name} exceeds the wire string limit.");
    }
}
