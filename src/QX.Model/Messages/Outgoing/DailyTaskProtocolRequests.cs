using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the user's daily tasks.</summary>
/// <remarks>Sent as the Flash <c>GetDailyTasks</c> message, which carries no fields.</remarks>
public sealed record DailyTaskListRequest : IParserComposer<DailyTaskListRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DailyTaskListRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DailyTaskListRequest ParseFlash(in PacketReader p)
    {
        DailyTaskWire.RequireEmpty(in p, nameof(DailyTaskListRequest));
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DailyTaskListRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);
}

/// <summary>Sent when the user claims the reward of a completed daily task.</summary>
/// <remarks>Sent as the Flash <c>ClaimDailyTask</c> message.</remarks>
/// <param name="TaskId">The id of the task to claim, written to the packet as a 32 bit integer.</param>
public sealed record DailyTaskClaimRequest(long TaskId)
    : IParserComposer<DailyTaskClaimRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DailyTaskClaimRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DailyTaskClaimRequest ParseFlash(in PacketReader p)
    {
        DailyTaskWire.RequireRemaining(in p, sizeof(int), 0, nameof(DailyTaskClaimRequest));
        var value = new DailyTaskClaimRequest(p.ReadInt());
        DailyTaskWire.RequireEmpty(in p, nameof(DailyTaskClaimRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DailyTaskClaimRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        p.WriteInt(unchecked((int)value.TaskId));
    }
}
