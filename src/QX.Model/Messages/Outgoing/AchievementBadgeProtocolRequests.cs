using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the user's achievement list.</summary>
/// <remarks>Sent as the Flash <c>GetAchievements</c> message, which carries no fields.</remarks>
public sealed record AchievementsRequest : IParserComposer<AchievementsRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AchievementsRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AchievementsRequest ParseFlash(in PacketReader p) => ParseEmpty(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AchievementsRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);

    private static AchievementsRequest ParseEmpty(in PacketReader p)
    {
        AchievementBadgeWire.RequireEmpty(in p, nameof(AchievementsRequest));
        return new();
    }
}

/// <summary>Requests the point limits that each achievement badge level needs.</summary>
/// <remarks>Sent as the Flash <c>GetBadgePointLimits</c> message, which carries no fields.</remarks>
public sealed record BadgePointLimitsRequest : IParserComposer<BadgePointLimitsRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static BadgePointLimitsRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static BadgePointLimitsRequest ParseFlash(in PacketReader p) => ParseEmpty(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(BadgePointLimitsRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);

    private static BadgePointLimitsRequest ParseEmpty(in PacketReader p)
    {
        AchievementBadgeWire.RequireEmpty(in p, nameof(BadgePointLimitsRequest));
        return new();
    }
}

/// <summary>Requests the user's badge inventory.</summary>
/// <remarks>Sent as the Flash <c>GetBadges</c> message, which carries no fields.</remarks>
public sealed record BadgeInventoryRequest : IParserComposer<BadgeInventoryRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static BadgeInventoryRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static BadgeInventoryRequest ParseFlash(in PacketReader p) => ParseEmpty(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(BadgeInventoryRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);

    private static BadgeInventoryRequest ParseEmpty(in PacketReader p)
    {
        AchievementBadgeWire.RequireEmpty(in p, nameof(BadgeInventoryRequest));
        return new();
    }
}
