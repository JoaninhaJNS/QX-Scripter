using Qx.Game;

namespace Qx.Scripting;

/// <content>
/// The phase of the trading window, on top of the main trade API.
/// </content>
public partial class ScriptGlobals
{
    /// <summary>
    /// Gets the phase of the open trade.
    /// </summary>
    /// <remarks>
    /// The phase is <see cref="Qx.Game.TradePhase.Idle"/> when no trade is open,
    /// <see cref="Qx.Game.TradePhase.Trading"/> while offers may still change, and
    /// <see cref="Qx.Game.TradePhase.AwaitingConfirmation"/> once both sides accepted and the
    /// offers are locked. It reverts to <see cref="Qx.Game.TradePhase.Trading"/> if either side
    /// withdraws their acceptance.
    /// </remarks>
    public TradePhase TradePhase => Trade.Active?.Phase ?? Qx.Game.TradePhase.Idle;

    /// <summary>
    /// Gets whether the trade has reached the final confirmation step, where the offers are
    /// locked and both sides still have to confirm.
    /// </summary>
    public bool IsTradeWaitingConfirmation =>
        Trade.Active?.Phase is Qx.Game.TradePhase.AwaitingConfirmation;

    /// <summary>
    /// Registers a handler that runs when the trade enters the final confirmation phase.
    /// </summary>
    /// <remarks>
    /// Same subscription as <see cref="OnTradeConfirmed"/>, under the name that matches the phase
    /// it reports.
    /// </remarks>
    /// <param name="handler">The handler to call, with no arguments.</param>
    /// <returns>
    /// A handle that removes the handler when disposed. The subscription is also removed when
    /// the script stops.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnTradeWaitingConfirm(Action handler) => OnTradeConfirmed(handler);
}
