using Qx;
using Qx.Game.Application;
using Qx.Messages;
using Qx.Model;
using Qx.Protocol;

namespace Qx.Scripting;

/// <summary>
/// Provides lookup of wire headers by message name for one direction.
/// </summary>
/// <remarks>
/// <para>
/// Indexing it is the shorthand behind <c>Out["Move"]</c> and <c>In["Chat"]</c>.
/// </para>
/// <para>
/// Resolution goes through the catalog loaded for the active session, so the same name can map
/// to different header values on different hotels or client builds. Never hard-code a header
/// number; look it up here, or use the constants on <see cref="Msg"/>.
/// </para>
/// </remarks>
/// <param name="messages">The message manager that resolves names against the active catalog.</param>
/// <param name="direction">The direction of the messages to resolve.</param>
public sealed class HeaderIndex(MessageManager messages, Direction direction)
{
    /// <summary>
    /// Gets the header the given message name resolves to on the active client.
    /// </summary>
    /// <param name="name">The message name as spelled in the catalog.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the name is not in the catalog for this direction and client. Unlike an intercept
    /// registration, which binds nothing and stays silent, a lookup failure is always thrown.
    /// </exception>
    public Header this[string name] =>
        messages.TryGetHeader(new Identifier(ClientType.None, direction, name), out Header header)
            ? header
            : throw new InvalidOperationException($"Unknown {(direction == Direction.Out ? "outgoing" : "incoming")} message '{name}'.");
}

public partial class ScriptGlobals
{
    private HeaderIndex? _out;
    private HeaderIndex? _in;

    private WalletStateView ReadWalletState(int? point_type = null, int point_limit = 1) =>
        Application.Invoke<WalletStateRequest, WalletStateView>(
            ApplicationMemberIds.WalletState,
            new WalletStateRequest(PointLimit: point_limit, PointType: point_type),
            Ct);

    private int ReadWalletPoint(int type) => WalletPoint(ReadWalletState(type), type);

    private static int WalletPoint(WalletStateView state, int type)
    {
        WalletPointBalance? point = state.ActivityPoints.Points.FirstOrDefault(
            candidate => candidate.Type == type);
        if (point is not null)
            return point.Amount;
        return state.PointsLoaded
            ? 0
            : throw new InvalidOperationException($"Activity point type {type} has not been loaded.");
    }

    /// <summary>
    /// Gets the header lookup for outgoing (client to server) messages, for example <c>Out["Move"]</c>.
    /// </summary>
    public HeaderIndex Out => _out ??= new HeaderIndex(Ext.Messages, Direction.Out);

    /// <summary>
    /// Gets the header lookup for incoming (server to client) messages, for example <c>In["Chat"]</c>.
    /// </summary>
    public HeaderIndex In => _in ??= new HeaderIndex(Ext.Messages, Direction.In);

    /// <summary>
    /// Gets whether the local user's own account data has been received.
    /// </summary>
    /// <remarks>
    /// Until it is, <see cref="UserId"/> is -1 and the other <c>User...</c> properties are empty.
    /// </remarks>
    public bool IsIdentityLoaded => Profile.Identity is not null;

    /// <summary>
    /// Gets whether a credit balance has been observed.
    /// </summary>
    /// <remarks>
    /// <see cref="Credits"/> reads 0 both for a genuinely empty wallet and for one that has not
    /// been reported yet; this tells the two apart.
    /// </remarks>
    public bool IsCreditsLoaded => ReadWalletState().CreditsLoaded;

    /// <summary>
    /// Gets whether the complete activity point balance snapshot has been observed.
    /// </summary>
    public bool IsPointsLoaded => ReadWalletState().PointsLoaded;

    /// <summary>Gets the local user's account id, or -1 before the identity has been received.</summary>
    public Id UserId => Profile.Identity?.Id ?? -1;

    /// <summary>Gets the local user's name, or an empty string before the identity has been received.</summary>
    public string UserName => Self?.Name ?? "";

    /// <summary>Gets the local user's figure string, or an empty string before the identity has been received.</summary>
    public string UserFigure => Self?.Figure ?? "";

    /// <summary>Gets the local user's motto, or an empty string before the identity has been received.</summary>
    public string UserMotto => Self?.Motto ?? "";

    /// <summary>
    /// Gets the local user's gender, or <see cref="Gender.Unisex"/> when the identity has not been
    /// received.
    /// </summary>
    public Gender UserGender => Self?.Gender ?? Gender.Unisex;

    /// <summary>
    /// Gets the game server host the session is connected to, for example <c>"game-de.habbo.com"</c>.
    /// </summary>
    /// <remarks>Empty before a connection has been observed.</remarks>
    public string Host => Session?.Host ?? "";

    /// <summary>
    /// Gets the website host matching <see cref="Host"/>, for example <c>"www.habbo.de"</c>.
    /// </summary>
    /// <remarks>
    /// Falls back to <c>"www.habbo.com"</c> for hosts that are not in the mapping table,
    /// including the empty host before a connection has been observed.
    /// </remarks>
    public string WebHost => Qx.Game.GameData.WebHostFor(Host);

    /// <summary>Gets the current room's id, or 0 when the user is not in a room.</summary>
    public long RoomId => Room.RoomId;

    /// <summary>
    /// Gets the credit balance last reported by the server.
    /// </summary>
    /// <remarks>
    /// Reads 0 until a credit balance has been seen, so check <see cref="IsCreditsLoaded"/>
    /// before trusting a zero.
    /// </remarks>
    public int Credits => ReadWalletState().Credits ?? 0;

    /// <summary>
    /// Gets the diamond balance (activity point type 5).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no diamond balance has been reported and the activity point snapshot has not been loaded.
    /// </exception>
    public int Diamonds => ReadWalletPoint(WalletPointTypes.Diamonds);

    /// <summary>
    /// Gets the ducket balance (activity point type 0).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no ducket balance has been reported and the activity point snapshot has not been loaded.
    /// </exception>
    public int Duckets => ReadWalletPoint(WalletPointTypes.Duckets);

    /// <summary>
    /// Gets the balance of an activity point currency.
    /// </summary>
    /// <remarks>
    /// A currency that is missing from a loaded activity point snapshot reads 0.
    /// </remarks>
    /// <param name="type">
    /// The currency type id: 0 for duckets, 5 for diamonds; hotels define further ids for
    /// seasonal currencies.
    /// </param>
    /// <returns>The reported balance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no balance for <paramref name="type"/> has been reported and the activity point snapshot
    /// has not been loaded.
    /// </exception>
    public int Points(int type) => ReadWalletPoint(type);

    /// <summary>
    /// Gets whether the script is still allowed to run.
    /// </summary>
    /// <remarks>
    /// Turns <see langword="false"/> as soon as the script is asked to stop, which makes it the
    /// idiomatic loop condition: <c>while (Run) { ... }</c>.
    /// </remarks>
    public bool Run => !Ct.IsCancellationRequested;

    /// <summary>
    /// Blocks the calling thread for the given number of milliseconds, waking early if the
    /// script is stopped.
    /// </summary>
    /// <param name="milliseconds">The time to sleep, in milliseconds.</param>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while sleeping.</exception>
    /// <remarks>
    /// It blocks a thread. Prefer <see cref="Delay(int)"/> inside async code; use <c>Sleep</c> in
    /// straight-line script bodies.
    /// </remarks>
    public void Sleep(int milliseconds)
    {
        if (Ct.WaitHandle.WaitOne(milliseconds))
            Ct.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Blocks the calling thread for the given interval, waking early if the script is stopped.
    /// </summary>
    /// <param name="timeout">The time to sleep.</param>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while sleeping.</exception>
    public void Sleep(TimeSpan timeout)
    {
        if (Ct.WaitHandle.WaitOne(timeout))
            Ct.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Sends a packet with the given header and values in the direction of the header.
    /// </summary>
    /// <remarks>
    /// Values are written in order: <see cref="int"/>, <see cref="string"/>, <see cref="bool"/>,
    /// <see cref="short"/>, <see cref="long"/>, <see cref="byte"/>, <see cref="float"/>,
    /// <see cref="double"/>, <see cref="char"/> (as a string), <see cref="Id"/>,
    /// <see cref="Length"/> and <see cref="IComposer"/> are supported. An outgoing header goes to
    /// the server, an incoming header to the game client.
    /// </remarks>
    /// <param name="header">The header of the message, usually looked up through <see cref="Out"/> or <see cref="In"/>.</param>
    /// <param name="values">The values to write into the packet body.</param>
    /// <exception cref="ArgumentException">Thrown when a value has an unsupported type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no connection is active, or the session changed before the packet could be sent.</exception>
    public void Send(Header header, params object[] values)
    {
        using var packet = new Packet(header, CurrentClient);
        packet.Writer().WriteValues(values);
        Ext.Send(packet);
    }

    /// <summary>
    /// Runs an action on a background thread, without waiting for it.
    /// </summary>
    /// <param name="action">The work to run. It is canceled together with the script.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use it for a loop that should keep going while the main script body does something else.
    /// </para>
    /// <para>
    /// The task is observed: an exception escaping it is reported as a script error and stops
    /// the run, rather than being swallowed. Cancellation is treated as a normal end, and
    /// <see cref="Finish"/> inside the task ends the whole run normally.
    /// </para>
    /// </remarks>
    public void RunTask(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        StartObservedTask(() => Task.Run(action, Ct), Ct);
    }

    /// <summary>
    /// Runs an asynchronous operation in the background, without waiting for it.
    /// </summary>
    /// <param name="action">The work to run. It is canceled together with the script.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The task is observed: an exception escaping it is reported as a script error and stops
    /// the run. Cancellation is treated as a normal end, and <see cref="Finish"/> inside the
    /// task ends the whole run normally.
    /// </remarks>
    public void RunTask(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        StartObservedTask(() => Task.Run(action, Ct), Ct);
    }

    private void StartObservedTask(Func<Task> action, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!TryTrackBackgroundTask(completion.Task, cancellationToken))
            return;
        _ = ObserveTask(action, cancellationToken, completion);
    }

    private async Task ObserveTask(
        Func<Task> action,
        CancellationToken cancellationToken,
        TaskCompletionSource completion)
    {
        using IDisposable scope = ScriptExecutionContext.Enter(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action().ConfigureAwait(false);
        }
        catch (ScriptFinishedException)
        {
            if (!cancellationToken.IsCancellationRequested)
                ReportBackgroundFinished();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            ReportBackgroundError(error);
        }
        finally
        {
            completion.TrySetResult();
        }
    }

    private void ReportBackgroundError(Exception error)
    {
        lock (_subscriptions)
            if (_disposed)
                return;

        if (_backgroundError is not null)
            _backgroundError(error);
        else
            _log(ScriptExecutionError.FromException(error, "background").Format());
    }

    /// <summary>
    /// Waits until the script is stopped.
    /// </summary>
    /// <remarks>
    /// Use it at the end of an event-driven script so the run stays alive while its handlers do
    /// the work.
    /// </remarks>
    /// <returns>A task that never completes successfully and is canceled when the script stops.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped.</exception>
    public Task Wait() => Task.Delay(Timeout.Infinite, Ct);

    /// <summary>
    /// Asynchronously waits for the given number of milliseconds.
    /// </summary>
    /// <remarks>Same as <see cref="Delay(int)"/>.</remarks>
    /// <param name="milliseconds">The time to wait, in milliseconds.</param>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while waiting.</exception>
    public Task DelayAsync(int milliseconds) => Task.Delay(milliseconds, Ct);

    /// <summary>
    /// Gets a random integer in the half-open range <c>[min, max)</c>.
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public int Rand(int min, int max) => Random.Shared.Next(min, max);

    /// <summary>Gets a random integer from 0 up to but not including <paramref name="max"/>.</summary>
    /// <param name="max">The exclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is negative.</exception>
    public int Rand(int max) => Random.Shared.Next(max);

    /// <summary>Gets a random double in the half-open range <c>[0, 1)</c>.</summary>
    public double RandDouble() => Random.Shared.NextDouble();

    /// <summary>
    /// Gets a random element of the sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">
    /// The candidates. A sequence that is not already a list is enumerated once into one.
    /// </param>
    /// <returns>A random element, or <c>default</c> when the sequence is empty.</returns>
    public T? Rand<T>(IEnumerable<T> items)
    {
        IList<T> list = items as IList<T> ?? items.ToList();
        return list.Count == 0 ? default : list[Random.Shared.Next(list.Count)];
    }
}
