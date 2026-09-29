using System.Diagnostics;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Waits until a condition holds. It is checked at once, again after every change to the room
    /// state, and at least every <paramref name="pollMs"/> milliseconds for conditions that depend
    /// on time or on the script's own flags.
    /// </summary>
    /// <param name="condition">
    /// What to wait for. It runs on whichever thread observed the change, so it should only read
    /// state. An exception it throws ends the wait and propagates to the caller.
    /// </param>
    /// <param name="timeoutMs">How long to wait in milliseconds; -1 waits without a limit.</param>
    /// <param name="pollMs">The longest gap between two checks, in milliseconds.</param>
    /// <returns><see langword="true"/> once the condition holds, <see langword="false"/> when the time ran out.</returns>
    /// <exception cref="OperationCanceledException">The script was stopped while waiting.</exception>
    public async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 10000, int pollMs = 50)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeoutMs, Timeout.Infinite);
        ArgumentOutOfRangeException.ThrowIfLessThan(pollMs, 1);
        CancellationToken cancellation_token = Ct;
        long started = Stopwatch.GetTimestamp();
        while (true)
        {
            cancellation_token.ThrowIfCancellationRequested();
            Task change = Room.NextChange;
            if (condition())
                return true;
            int wait = pollMs;
            if (timeoutMs != Timeout.Infinite)
            {
                long remaining = timeoutMs - (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if (remaining <= 0)
                    return false;
                wait = (int)Math.Min(wait, remaining);
            }
            await Task.WhenAny(change, Task.Delay(wait, cancellation_token)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until a room is fully loaded with the own avatar and the furni in it.
    /// </summary>
    /// <param name="roomId">The room to wait for; 0 accepts whichever room is entered.</param>
    /// <param name="timeoutMs">How long to wait in milliseconds; -1 waits without a limit.</param>
    /// <returns><see langword="true"/> once the room is ready, <see langword="false"/> when the time ran out.</returns>
    /// <exception cref="OperationCanceledException">The script was stopped while waiting.</exception>
    public Task<bool> WaitRoomReady(long roomId = 0, int timeoutMs = 20000) =>
        WaitUntil(
            () => Room.Capture(room =>
                room.IsReady &&
                room.FloorItemsAreLoaded &&
                room.Self is not null &&
                (roomId == 0 || room.RoomId == roomId)),
            timeoutMs);

    /// <summary>
    /// Binds to the room the local user is in right now, so later work can tell whether it is
    /// still the same visit. See <see cref="RoomScope"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No room is loaded or the own avatar is not in it.</exception>
    public RoomScope CaptureRoom() => RoomScope.Capture(Room);
}
