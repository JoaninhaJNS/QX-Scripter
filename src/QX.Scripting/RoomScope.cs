using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>Thrown when work bound to a <see cref="RoomScope"/> finds that its room session has ended.</summary>
public sealed class RoomChangedException(string message) : InvalidOperationException(message);

/// <summary>
/// One visit to one room: the room session and the local user's avatar in it as they were when
/// the scope was captured. It stays current until the room is left or entered again, or the own
/// avatar leaves it, and lets a script bind its work to that one visit.
/// </summary>
public sealed class RoomScope
{
    readonly RoomManager _room;

    internal RoomScope(RoomManager room, long room_id, long generation, int self_index, CancellationToken token)
    {
        _room = room;
        RoomId = room_id;
        Generation = generation;
        SelfIndex = self_index;
        Token = token;
    }

    /// <summary>The room the scope belongs to.</summary>
    public long RoomId { get; }

    /// <summary>The room session generation the scope belongs to.</summary>
    public long Generation { get; }

    /// <summary>The room index of the local user's avatar during this visit.</summary>
    public int SelfIndex { get; }

    /// <summary>Cancelled as soon as the room session ends, whether by leaving or by entering again.</summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Whether the visit is still going on: the same room session is still loaded and the own
    /// avatar is still in it under the same index.
    /// </summary>
    public bool IsCurrent => _room.Capture(room =>
        room.Generation == Generation &&
        room.IsReady &&
        room.RoomId == RoomId &&
        room.Self is { } self &&
        self.Index == SelfIndex);

    /// <summary>Throws a <see cref="RoomChangedException"/> when the visit is over.</summary>
    public void ThrowIfChanged()
    {
        if (!IsCurrent)
            throw new RoomChangedException($"Room {RoomId} was left or its session changed.");
    }

    internal static RoomScope Capture(RoomManager room) => room.Capture(state =>
    {
        if (!state.IsReady || state.Self is not { } self)
            throw new InvalidOperationException("A room scope needs a loaded room with the own avatar in it.");
        return new RoomScope(room, state.RoomId, state.Generation, self.Index, state.SessionToken);
    });
}
