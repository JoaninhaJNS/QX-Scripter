using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Specifies why the server refused a room connection, as classified from <see cref="CanNotConnect.ReasonCode"/>.</summary>
public enum RoomConnectionFailureKind
{
    /// <summary>A reason code with no known meaning.</summary>
    Unknown,
    /// <summary>The room is full, reason code 1.</summary>
    Full,
    /// <summary>The room queue failed, reason code 3.</summary>
    QueueError,
    /// <summary>The user is banned from the room, reason code 4.</summary>
    Banned,
    /// <summary>The user is blocked from the room, reason code 5.</summary>
    Blocked
}

/// <summary>Specifies what entering a room queue set grants.</summary>
public enum RoomQueueTarget
{
    /// <summary>Entry as a spectator.</summary>
    Spectator = 1,
    /// <summary>Entry as a visitor.</summary>
    Visitor = 2
}

/// <summary>Represents the <c>OpenConnection</c> message, received when the server confirms the user's connection to a room.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record OpenConnectionConfirmation(Id RoomId) : IParserComposer<OpenConnectionConfirmation>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static OpenConnectionConfirmation Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static OpenConnectionConfirmation ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(OpenConnectionConfirmation value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}

/// <summary>Represents the <c>FlatAccessible</c> message, received when the server grants access to a room, to the user or to someone at the doorbell.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="UserName">The name of the user who was let in, or an empty string when the access is for the user.</param>
public sealed record FlatAccessible(Id RoomId, string UserName) : IParserComposer<FlatAccessible>
{
    /// <summary>Gets whether the access is for the user, which is the case when <see cref="UserName"/> is empty.</summary>
    public bool IsSelf => string.IsNullOrEmpty(UserName);

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FlatAccessible Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FlatAccessible ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FlatAccessible value, in PacketWriter p)
    {
        p.WriteId(value.RoomId);
        p.WriteString(value.UserName);
    }
}

/// <summary>Represents the <c>FlatAccessDenied</c> message, received when access to a room is refused, to the user or to someone at the doorbell.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="UserName">
/// The name of the user who was refused, an empty string when the refusal is for the user, or
/// <see langword="null"/> when the packet ends after the room ID.
/// </param>
public sealed record FlatAccessDenied(Id RoomId, string? UserName) : IParserComposer<FlatAccessDenied>
{
    /// <summary>Gets whether the refusal is for the user, which is the case when <see cref="UserName"/> is <see langword="null"/> or empty.</summary>
    public bool IsSelf => string.IsNullOrEmpty(UserName);

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FlatAccessDenied Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FlatAccessDenied ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadId();
        return new FlatAccessDenied(room_id, p.Available > 0 ? p.ReadString() : null);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FlatAccessDenied value, in PacketWriter p)
    {
        p.WriteId(value.RoomId);
        if (value.UserName is not null)
            p.WriteString(value.UserName);
    }
}

/// <summary>Represents the <c>NoSuchFlat</c> message, received when the room the user tried to enter does not exist.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record NoSuchFlat(Id RoomId) : IParserComposer<NoSuchFlat>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NoSuchFlat Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NoSuchFlat ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NoSuchFlat value, in PacketWriter p) =>
        p.WriteId(value.RoomId);
}

/// <summary>Represents the <c>CantConnect</c> message, received when the server refuses the user's connection to a room.</summary>
/// <param name="ReasonCode">The raw reason code: 1 room full, 3 queue error, 4 banned, 5 blocked.</param>
/// <param name="Parameter">The extra text sent with a queue error, reason code 3, and an empty string otherwise.</param>
public sealed record CanNotConnect(int ReasonCode, string Parameter) : IParserComposer<CanNotConnect>
{
    /// <summary>Gets the reason classified from <see cref="ReasonCode"/>, <see cref="RoomConnectionFailureKind.Unknown"/> for any code without a known meaning.</summary>
    public RoomConnectionFailureKind Kind => ReasonCode switch
    {
        1 => RoomConnectionFailureKind.Full,
        3 => RoomConnectionFailureKind.QueueError,
        4 => RoomConnectionFailureKind.Banned,
        5 => RoomConnectionFailureKind.Blocked,
        _ => RoomConnectionFailureKind.Unknown
    };

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CanNotConnect Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CanNotConnect ParseFlash(in PacketReader p)
    {
        int reason_code = p.ReadInt();
        return new CanNotConnect(reason_code, reason_code == 3 ? p.ReadString() : "");
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CanNotConnect value, in PacketWriter p)
    {
        p.WriteInt(value.ReasonCode);
        if (value.ReasonCode == 3)
            p.WriteString(value.Parameter);
    }
}

/// <summary>Represents one queue of a room queue set.</summary>
/// <param name="Type">The queue's identifier as sent by the hotel, for example <c>visitors</c>.</param>
/// <param name="Size">The user's zero based place in this queue, or a negative value when the hotel reports no place.</param>
public sealed record RoomQueueEntry(string Type, int Size) : IParserComposer<RoomQueueEntry>
{
    /// <summary>Parses the queue from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomQueueEntry Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomQueueEntry ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadInt());

    /// <summary>Composes the queue into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomQueueEntry value, in PacketWriter p)
    {
        p.WriteString(value.Type);
        p.WriteInt(value.Size);
    }
}

/// <summary>Represents a group of room queues that share one entry target.</summary>
/// <param name="Name">The set's name as sent by the hotel.</param>
/// <param name="Target">What entering through this set grants.</param>
/// <param name="Queues">The queues that make up the set.</param>
public sealed record RoomQueueSet(
    string Name,
    RoomQueueTarget Target,
    IReadOnlyList<RoomQueueEntry> Queues) : IParserComposer<RoomQueueSet>
{
    /// <summary>Gets the user's one based place in the first queue, or <see langword="null"/> when there is no queue or the hotel reports no place.</summary>
    public int? Position => Queues.Count == 0 || Queues[0].Size < 0
        ? null
        : Queues[0].Size == int.MaxValue ? int.MaxValue : Queues[0].Size + 1;

    /// <summary>Parses the set from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomQueueSet Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomQueueSet ParseFlash(in PacketReader p)
    {
        string name = p.ReadString();
        var target = (RoomQueueTarget)p.ReadInt();
        int count = p.ReadLength();
        var queues = new RoomQueueEntry[count];
        for (int index = 0; index < count; index++)
            queues[index] = p.Parse<RoomQueueEntry>();
        return new RoomQueueSet(name, target, queues);
    }

    /// <summary>Composes the set into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomQueueSet value, in PacketWriter p)
    {
        p.WriteString(value.Name);
        p.WriteInt((int)value.Target);
        p.WriteLength((Length)value.Queues.Count);
        foreach (RoomQueueEntry queue in value.Queues)
            p.Compose(queue);
    }
}

/// <summary>Represents the <c>RoomQueueStatus</c> message, received with the user's place in the queues of a room they are waiting to enter.</summary>
/// <param name="RoomId">The ID of the room being queued for.</param>
/// <param name="Sets">Every queue set the hotel reported for the room.</param>
public sealed record RoomQueueStatus(Id RoomId, IReadOnlyList<RoomQueueSet> Sets)
    : IParserComposer<RoomQueueStatus>
{
    /// <summary>Gets the target of the first set, which is the set being waited in, or <see langword="null"/> when there are no sets.</summary>
    public RoomQueueTarget? ActiveTarget => Sets.Count == 0 ? null : Sets[0].Target;
    /// <summary>Gets the first set with <see cref="ActiveTarget"/> as its target, or <see langword="null"/> when there are no sets.</summary>
    public RoomQueueSet? ActiveSet => ActiveTarget is { } target
        ? Sets.FirstOrDefault(set => set.Target == target)
        : null;
    /// <summary>Gets the user's one based place in the active set, or <see langword="null"/> when there is none.</summary>
    public int? Position => ActiveSet?.Position;

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomQueueStatus Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomQueueStatus ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadId();
        int count = p.ReadLength();
        var sets = new RoomQueueSet[count];
        for (int index = 0; index < count; index++)
            sets[index] = p.Parse<RoomQueueSet>();
        return new RoomQueueStatus(room_id, sets);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomQueueStatus value, in PacketWriter p)
    {
        p.WriteId(value.RoomId);
        p.WriteLength((Length)value.Sets.Count);
        foreach (RoomQueueSet set in value.Sets)
            p.Compose(set);
    }
}
