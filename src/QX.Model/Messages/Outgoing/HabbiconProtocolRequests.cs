using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the habbicon shop, which lists every collection with the habbicons it holds.</summary>
/// <remarks>Sent as the Flash <c>GetHabbiconShopData</c> message, which carries no fields.</remarks>
public sealed record HabbiconShopRequest : IParserComposer<HabbiconShopRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconShopRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseMessage);

    private static HabbiconShopRequest ParseMessage(in PacketReader p)
    {
        HabbiconWire.RequireEmpty(in p, nameof(HabbiconShopRequest));
        return new HabbiconShopRequest();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeMessage);

    private static void ComposeMessage(HabbiconShopRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);
}

/// <summary>Requests the details of a habbicon.</summary>
/// <remarks>Sent as the Flash <c>GetHabbiconInfo</c> message.</remarks>
/// <param name="HabbiconId">The id of the habbicon.</param>
public sealed record HabbiconInfoRequest(int HabbiconId) : IParserComposer<HabbiconInfoRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconInfoRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseMessage);

    private static HabbiconInfoRequest ParseMessage(in PacketReader p) =>
        new(ReadInt(in p, nameof(HabbiconInfoRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeMessage);

    private static void ComposeMessage(HabbiconInfoRequest value, in PacketWriter p) =>
        WriteInt(value, value.HabbiconId, in p);

    internal static int ReadInt(in PacketReader p, string name)
    {
        HabbiconWire.RequireRemaining(in p, sizeof(int), 0, name);
        int value = p.ReadInt();
        HabbiconWire.RequireEmpty(in p, name);
        return value;
    }

    internal static void WriteInt<T>(T value, int id, in PacketWriter p)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        p.WriteInt(id);
    }
}

/// <summary>Sent when the user buys a habbicon.</summary>
/// <remarks>Sent as the Flash <c>BuyHabbicon</c> message.</remarks>
/// <param name="HabbiconId">The id of the habbicon to buy.</param>
public sealed record HabbiconBuyRequest(int HabbiconId) : IParserComposer<HabbiconBuyRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconBuyRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HabbiconBuyRequest ParseFlash(in PacketReader p) =>
        new(HabbiconInfoRequest.ReadInt(in p, nameof(HabbiconBuyRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HabbiconBuyRequest value, in PacketWriter p) =>
        HabbiconInfoRequest.WriteInt(value, value.HabbiconId, in p);
}

/// <summary>Sent when the user buys a whole habbicon collection.</summary>
/// <remarks>Sent as the Flash <c>BuyHabbiconCollection</c> message.</remarks>
/// <param name="CollectionId">The id of the collection to buy.</param>
public sealed record HabbiconCollectionBuyRequest(int CollectionId)
    : IParserComposer<HabbiconCollectionBuyRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconCollectionBuyRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HabbiconCollectionBuyRequest ParseFlash(in PacketReader p) =>
        new(HabbiconInfoRequest.ReadInt(in p, nameof(HabbiconCollectionBuyRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HabbiconCollectionBuyRequest value, in PacketWriter p) =>
        HabbiconInfoRequest.WriteInt(value, value.CollectionId, in p);
}

/// <summary>Sent when the user claims an earned habbicon.</summary>
/// <remarks>Sent as the Flash <c>ClaimHabbicon</c> message.</remarks>
/// <param name="HabbiconId">The id of the habbicon to claim.</param>
public sealed record HabbiconClaimRequest(int HabbiconId) : IParserComposer<HabbiconClaimRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconClaimRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HabbiconClaimRequest ParseFlash(in PacketReader p) =>
        new(HabbiconInfoRequest.ReadInt(in p, nameof(HabbiconClaimRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HabbiconClaimRequest value, in PacketWriter p) =>
        HabbiconInfoRequest.WriteInt(value, value.HabbiconId, in p);
}

/// <summary>Sent when the user marks an owned habbicon as a favorite.</summary>
/// <remarks>Sent as the Flash <c>FavoriteHabbicon</c> message.</remarks>
/// <param name="HabbiconId">The id of the habbicon.</param>
public sealed record HabbiconFavoriteRequest(int HabbiconId)
    : IParserComposer<HabbiconFavoriteRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconFavoriteRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HabbiconFavoriteRequest ParseFlash(in PacketReader p) =>
        new(HabbiconInfoRequest.ReadInt(in p, nameof(HabbiconFavoriteRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HabbiconFavoriteRequest value, in PacketWriter p) =>
        HabbiconInfoRequest.WriteInt(value, value.HabbiconId, in p);
}

/// <summary>Sent when the user removes a habbicon from the favorites.</summary>
/// <remarks>Sent as the Flash <c>UnfavoriteHabbicon</c> message.</remarks>
/// <param name="HabbiconId">The id of the habbicon.</param>
public sealed record HabbiconUnfavoriteRequest(int HabbiconId)
    : IParserComposer<HabbiconUnfavoriteRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static HabbiconUnfavoriteRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static HabbiconUnfavoriteRequest ParseFlash(in PacketReader p) =>
        new(HabbiconInfoRequest.ReadInt(in p, nameof(HabbiconUnfavoriteRequest)));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(HabbiconUnfavoriteRequest value, in PacketWriter p) =>
        HabbiconInfoRequest.WriteInt(value, value.HabbiconId, in p);
}
