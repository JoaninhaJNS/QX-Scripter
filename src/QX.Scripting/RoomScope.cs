using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>Thrown when work bound to a <see cref="RoomScope"/> finds that its room session has ended.</summary>
/// <param name="message">The message that describes the error.</param>
public sealed class RoomChangedException(string message) : InvalidOperationException(message);

/// <summary>
/// Represents one visit to one room: the room session and the local user's avatar in it as they
/// were when the scope was captured.
/// </summary>
/// <remarks>
/// It stays current until the room is left or entered again, or the own avatar leaves it, and lets
/// a script bind its work to that one visit. Scripts obtain one from
/// <see cref="ScriptGlobals.CaptureRoom"/>.
/// </remarks>
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

    /// <summary>Gets the ID of the room the scope belongs to.</summary>
    public long RoomId { get; }

    /// <summary>Gets the room session generation the scope belongs to.</summary>
    public long Generation { get; }

    /// <summary>Gets the room index of the local user's avatar during this visit.</summary>
    public int SelfIndex { get; }

    /// <summary>
    /// Gets a token that is canceled as soon as the room session ends, whether by leaving or by
    /// entering again.
    /// </summary>
    /// <remarks>The token is not canceled when only the own avatar leaves the room; check <see cref="IsCurrent"/> for that.</remarks>
    public CancellationToken Token { get; }

    /// <summary>
    /// Gets whether the visit is still going on: the same room session is still loaded and the own
    /// avatar is still in it under the same index.
    /// </summary>
    public bool IsCurrent => _room.Capture(room =>
        room.Generation == Generation &&
        room.IsReady &&
        room.RoomId == RoomId &&
        room.Self is { } self &&
        self.Index == SelfIndex);

    /// <summary>Throws a <see cref="RoomChangedException"/> when the visit is over.</summary>
    /// <exception cref="RoomChangedException">Thrown when the room was left, its session changed, or the own avatar is no longer in it under the same index.</exception>
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
