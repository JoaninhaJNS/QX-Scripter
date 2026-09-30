using Qx.Game;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

/// <content>
/// Room access: how far the local user got trying to enter a room (connecting, ringing a
/// doorbell, waiting in a queue, admitted, denied, not found, or refused outright), plus the
/// correlated entry helper and the access events.
/// <para>
/// All of the state below is a live view of the room tracker, updated as the entry handshake
/// progresses. Reading it never sends anything.
/// </para>
/// </content>
public partial class ScriptGlobals
{
    /// <summary>
    /// Gets where the local user stands in the room entry handshake.
    /// </summary>
    /// <remarks>
    /// The states are <c>Idle</c>, <c>Connecting</c>, <c>RingingDoorbell</c>, <c>Queued</c>,
    /// <c>Accessible</c>, and the terminal failures <c>Denied</c>, <c>NotFound</c> and
    /// <c>ConnectionError</c>.
    /// </remarks>
    public RoomAccessState RoomAccessState => Room.AccessState;

    /// <summary>
    /// Gets the room the current access state refers to, or <see langword="null"/> when the state
    /// is idle or the server never named a room.
    /// </summary>
    /// <remarks>
    /// This is not necessarily the room the user is in; it is the room being entered.
    /// </remarks>
    public Id? RoomAccessRoomId => Room.AccessRoomId;

    /// <summary>
    /// Gets whether the local user is waiting at a locked door for someone inside to let them in.
    /// </summary>
    public bool IsRingingDoorbell => Room.IsRingingDoorbell;

    /// <summary>Gets whether the local user is currently waiting in a room's door queue.</summary>
    public bool IsInRoomQueue => Room.IsInQueue;

    /// <summary>
    /// Gets the local user's place in the queue they are waiting in, or <see langword="null"/>
    /// when they are not queued.
    /// </summary>
    /// <remarks>
    /// The value comes from the queue set whose target matches the first set the server reports.
    /// </remarks>
    public int? RoomQueuePosition => Room.QueuePosition;

    /// <summary>
    /// Gets the full queue status as last reported, or <see langword="null"/> when there is none.
    /// </summary>
    /// <remarks>
    /// It holds the room and every queue set with its target (spectator or visitor) and position.
    /// It is <see langword="null"/> until a queue message arrives, and is cleared when the access
    /// state moves to a state other than queued.
    /// </remarks>
    public RoomQueueStatus? CurrentRoomQueue => Room.QueueStatus;

    /// <summary>
    /// Gets why the last room entry attempt was refused outright, or <see langword="null"/> when
    /// the current attempt did not fail that way.
    /// </summary>
    /// <remarks>
    /// The failure names the reason (room full, queue error, banned or blocked) together with the
    /// raw reason code. It is cleared on every access state change.
    /// </remarks>
    public RoomConnectionFailure? LastRoomConnectionFailure => Room.ConnectionFailure;

    /// <summary>
    /// Requests entry into a room and waits for the handshake to reach a conclusion.
    /// </summary>
    /// <param name="room_id">The room to enter. Must be positive.</param>
    /// <param name="password">The door password; empty for rooms that need none.</param>
    /// <param name="timeout_ms">
    /// The timeout in milliseconds for the handshake to conclude. Doorbell and queue waits count
    /// against this budget, so a busy room usually needs more than the default.
    /// </param>
    /// <param name="cancellation_token">
    /// An extra token to abandon the wait with. The script's own stop token always applies as well.
    /// </param>
    /// <returns>
    /// The outcome (success, denied, not found, or connection error) with the room id, the failure
    /// detail for a refused connection, and the exit when the room was left before entry completed.
    /// A connection error is also reported when the hotel connection closes during the attempt.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="room_id"/> is not positive, or <paramref name="timeout_ms"/> is not positive.
    /// </exception>
    /// <exception cref="Qx.Game.RoomEntryTimeoutException">
    /// Thrown when the handshake did not conclude in time. This is a <see cref="TimeoutException"/>.
    /// </exception>
    /// <exception cref="Qx.Game.RoomEntryReplacedException">
    /// Thrown when another room entry was started before this one finished. Only one entry attempt is tracked
    /// at a time.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the script was stopped, or <paramref name="cancellation_token"/> was canceled.
    /// </exception>
    /// <remarks>
    /// Only failures the server reports as an access result are returned. A wrong password is not
    /// one of them: the server answers with a generic error and does not let the user in, so the
    /// call ends in a timeout.
    /// </remarks>
    public async Task<RoomEntryResult> EnsureEnterRoom(
        Id room_id,
        string password = "",
        int timeout_ms = 10000,
        CancellationToken cancellation_token = default)
    {
        ArgumentNullException.ThrowIfNull(password);
        CancellationToken script_token = Ct;
        using var operation_lifetime = new CancellationTokenSource();
        using IDisposable tracked_lifetime = Track(new Unsubscriber(operation_lifetime.Cancel));
        using CancellationTokenSource linked = cancellation_token.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(
                script_token,
                cancellation_token,
                operation_lifetime.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(
                script_token,
                operation_lifetime.Token);
        try
        {
            return await Game.RoomEntries
                .EnsureAsync(
                    room_id,
                    () => EnterRoom(room_id, password),
                    timeout_ms,
                    linked.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellation_token);
        }
        catch (OperationCanceledException) when (script_token.IsCancellationRequested)
        {
            throw new OperationCanceledException(script_token);
        }
    }

    /// <summary>
    /// Registers a handler that runs on every room access state change.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the transition, which carries the old and new state and room id,
    /// plus the failure detail when the new state is a connection error.
    /// </param>
    /// <returns>
    /// A handle that removes the handler when disposed. The subscription is also torn down when
    /// the script stops, so the handle only has to be kept to unsubscribe earlier.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomAccessChanged(Action<RoomAccessTransition> handler)
        => Subscribe(
            handler,
            value => Room.AccessStateChanged += value,
            value => Room.AccessStateChanged -= value);

    /// <summary>
    /// Registers a handler that runs each time the server sends a door queue update.
    /// </summary>
    /// <remarks>
    /// The server sends one whenever the local user's place in the door queue moves.
    /// </remarks>
    /// <param name="handler">The handler to call with the queue status, including every queue set.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomQueueUpdated(Action<RoomQueueStatus> handler)
        => Subscribe(
            handler,
            value => Room.QueueUpdated += value,
            value => Room.QueueUpdated -= value);

    /// <summary>
    /// Registers a handler that runs when the server refuses a room connection outright.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the refusal, which carries the raw reason code and, for a queue
    /// error, the queue name. Its kind maps 1 to full, 3 to queue error, 4 to banned and 5 to blocked.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomConnectionFailed(Action<CanNotConnect> handler)
        => Subscribe(
            handler,
            value => Room.ConnectionFailed += value,
            value => Room.ConnectionFailed -= value);

    /// <summary>
    /// Registers a handler that runs when a doorbell rings.
    /// </summary>
    /// <remarks>
    /// This covers both directions: an empty user name means the local user is the one waiting
    /// outside, a non-empty one names a visitor waiting at the door of the room the local user is in.
    /// </remarks>
    /// <param name="handler">The handler to call with the doorbell message.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnDoorbell(Action<Doorbell> handler)
        => Subscribe(
            handler,
            value => Room.DoorbellRang += value,
            value => Room.DoorbellRang -= value);

    /// <summary>
    /// Registers a handler that runs when the server grants access through a room door.
    /// </summary>
    /// <remarks>
    /// This covers both directions: an empty user name (<see cref="FlatAccessible.IsSelf"/>) means
    /// the local user was let in, a non-empty one names a visitor let into the room the local user
    /// is in.
    /// </remarks>
    /// <param name="handler">The handler to call with the room id and user name.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomAccessGranted(Action<FlatAccessible> handler)
    => Subscribe(
        handler,
        value => Room.AccessGranted += value,
        value => Room.AccessGranted -= value);

    /// <summary>
    /// Registers a handler that runs when the server denies access through a room door.
    /// </summary>
    /// <remarks>
    /// This covers both directions: an empty user name (<see cref="FlatAccessDenied.IsSelf"/>)
    /// means the local user was turned away, a non-empty one names a visitor who was turned away
    /// from the room the local user is in.
    /// </remarks>
    /// <param name="handler">The handler to call with the room id and user name.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomAccessDenied(Action<FlatAccessDenied> handler)
    => Subscribe(
        handler,
        value => Room.AccessDenied += value,
        value => Room.AccessDenied -= value);
}
