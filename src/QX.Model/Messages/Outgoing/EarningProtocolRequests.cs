using Qx.Messages;
using Qx.Model.Messages.Incoming;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the status of the user's unclaimed earnings.</summary>
/// <remarks>Sent as the Flash <c>IncomeRewardStatus</c> message, which carries no fields.</remarks>
public sealed record EarningStatusRequest : IParserComposer<EarningStatusRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static EarningStatusRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static EarningStatusRequest ParseFlash(in PacketReader p) => ParseEmpty(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(EarningStatusRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);

    private static EarningStatusRequest ParseEmpty(in PacketReader p)
    {
        EarningWire.RequireEmpty(in p, nameof(EarningStatusRequest));
        return new();
    }
}

/// <summary>Sent when the user claims the earnings of a category.</summary>
/// <remarks>Sent as the Flash <c>IncomeRewardClaim</c> message.</remarks>
/// <param name="Category">The category to claim, written as a signed byte, or <see cref="EarningCategory.All"/> to claim every category.</param>
public sealed record EarningClaimRequest(EarningCategory Category)
    : IParserComposer<EarningClaimRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static EarningClaimRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static EarningClaimRequest ParseFlash(in PacketReader p) => ParseMessage(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(EarningClaimRequest value, in PacketWriter p) =>
        ComposeMessage(value, in p);

    private static EarningClaimRequest ParseMessage(in PacketReader p)
    {
        EarningWire.RequireRemaining(in p, sizeof(byte), 0, nameof(EarningClaimRequest));
        var value = new EarningClaimRequest((EarningCategory)(sbyte)p.ReadByte());
        EarningWire.RequireEmpty(in p, nameof(EarningClaimRequest));
        return value;
    }

    private static void ComposeMessage(EarningClaimRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        p.WriteByte(unchecked((byte)(sbyte)value.Category));
    }
}
