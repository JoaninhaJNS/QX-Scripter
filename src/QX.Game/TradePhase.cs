namespace Qx.Game;

/// <summary>
/// Specifies the phase of a trade.
/// </summary>
public enum TradePhase
{
    /// <summary>No open trade.</summary>
    Idle,
    /// <summary>An open trade whose offers can still change.</summary>
    Trading,
    /// <summary>A trade the server asked both users to confirm.</summary>
    AwaitingConfirmation
}
