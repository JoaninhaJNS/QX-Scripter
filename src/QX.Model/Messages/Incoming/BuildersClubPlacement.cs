using Qx.Messages;
using Qx.Model;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>BuildersClubPlaceRoomItem</c> message, sent to place a Builders Club floor item in the room.
/// </summary>
/// <param name="PageId">The identifier of the catalog page with the offer.</param>
/// <param name="OfferId">The identifier of the Builders Club offer.</param>
/// <param name="ExtraData">The offer selection data.</param>
/// <param name="X">The tile x coordinate.</param>
/// <param name="Y">The tile y coordinate.</param>
/// <param name="Direction">The item direction.</param>
/// <param name="IsRetry">Whether the placement is flagged to the hotel as a retry.</param>
public sealed record BuildersClubPlaceRoomItem(
    int PageId,
    int OfferId,
    string ExtraData,
    int X,
    int Y,
    int Direction,
    bool IsRetry = false) : IParserComposer<BuildersClubPlaceRoomItem>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static BuildersClubPlaceRoomItem Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static BuildersClubPlaceRoomItem ParseFlash(in PacketReader p) => ParseRequest(in p);

    private static BuildersClubPlaceRoomItem ParseRequest(in PacketReader p)
    {
        SubscriptionAdjunctWire.RequireMinimum(in p, 23, nameof(BuildersClubPlaceRoomItem));
        var value = new BuildersClubPlaceRoomItem(
            p.ReadInt(),
            p.ReadInt(),
            p.ReadString(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadBool());
        SubscriptionAdjunctWire.RequireEmpty(in p, nameof(BuildersClubPlaceRoomItem));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(BuildersClubPlaceRoomItem value, in PacketWriter p) =>
        ComposeRequest(value, in p);

    private static void ComposeRequest(BuildersClubPlaceRoomItem value, in PacketWriter p)
    {
        SubscriptionAdjunctWire.RequireString(value.ExtraData, nameof(ExtraData), in p);
        p.WriteInt(value.PageId);
        p.WriteInt(value.OfferId);
        p.WriteString(value.ExtraData);
        p.WriteInt(value.X);
        p.WriteInt(value.Y);
        p.WriteInt(value.Direction);
        p.WriteBool(value.IsRetry);
    }
}

/// <summary>
/// Represents the <c>BuildersClubPlaceWallItem</c> message, sent to place a Builders Club wall item in the room.
/// </summary>
/// <param name="PageId">The identifier of the catalog page with the offer.</param>
/// <param name="OfferId">The identifier of the Builders Club offer.</param>
/// <param name="ExtraData">The offer selection data.</param>
/// <param name="WallLocation">The wall location as a string in the Flash client's format.</param>
/// <param name="IsRetry">Whether the placement is flagged to the hotel as a retry.</param>
public sealed record BuildersClubPlaceWallItem(
    int PageId,
    int OfferId,
    string ExtraData,
    string WallLocation,
    bool IsRetry = false) : IParserComposer<BuildersClubPlaceWallItem>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static BuildersClubPlaceWallItem Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static BuildersClubPlaceWallItem ParseFlash(in PacketReader p)
    {
        SubscriptionAdjunctWire.RequireMinimum(in p, 13, nameof(BuildersClubPlaceWallItem));
        var value = new BuildersClubPlaceWallItem(
            p.ReadInt(),
            p.ReadInt(),
            p.ReadString(),
            p.ReadString(),
            p.ReadBool());
        SubscriptionAdjunctWire.RequireEmpty(in p, nameof(BuildersClubPlaceWallItem));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(BuildersClubPlaceWallItem value, in PacketWriter p)
    {
        SubscriptionAdjunctWire.RequireString(value.ExtraData, nameof(ExtraData), in p);
        SubscriptionAdjunctWire.RequireString(value.WallLocation, nameof(WallLocation), in p);
        p.WriteInt(value.PageId);
        p.WriteInt(value.OfferId);
        p.WriteString(value.ExtraData);
        p.WriteString(value.WallLocation);
        p.WriteBool(value.IsRetry);
    }
}
