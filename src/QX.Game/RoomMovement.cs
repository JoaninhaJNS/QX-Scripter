using Qx.Model;

namespace Qx.Game;

/// <summary>What moved an avatar or a floor item.</summary>
public enum MovementSource
{
    /// <summary>A status update: the avatar walked, stopped, or had its position restated.</summary>
    Walk,

    /// <summary>A roller slid the avatar or the item to the next tile.</summary>
    Roller,

    /// <summary>A wired effect moved, teleported or rotated it.</summary>
    Wired,

    /// <summary>The item was moved or rotated by someone with rights.</summary>
    Update
}

/// <summary>
/// One movement of an avatar as the server reported it. Unlike an avatar's changed location, a
/// movement is raised for every status update, so a walk that starts, continues or stops on the
/// same tile is still observed, and it says what caused it.
/// </summary>
/// <param name="Index">The avatar's room index.</param>
/// <param name="IsSelf">Whether the avatar is the local user's own.</param>
/// <param name="From">The tile the avatar was on before this movement.</param>
/// <param name="To">The tile the avatar is on now.</param>
/// <param name="MovingTo">
/// For <see cref="MovementSource.Walk"/>, the tile the avatar is stepping onto next, or
/// <see langword="null"/> when it stands still. Always <see langword="null"/> for rollers and wired.
/// </param>
/// <param name="Source">What caused the movement.</param>
/// <param name="Duration">How long the client animates it, in milliseconds; 0 when the server does not say.</param>
/// <param name="Revision">
/// The room revision the movement was applied at. Compare it with <see cref="RoomManager.Revision"/>
/// read before sending a command to tell movements caused by that command from older ones.
/// </param>
/// <param name="Timestamp">When the movement was received, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
public sealed record AvatarMovement(
    int Index,
    bool IsSelf,
    Tile From,
    Tile To,
    Tile? MovingTo,
    MovementSource Source,
    int Duration,
    long Revision,
    long Timestamp)
{
    /// <summary>Whether the avatar ended up on a different tile.</summary>
    public bool Relocated => From.XY != To.XY;

    /// <summary>Whether this is a walk update that shows the avatar standing still.</summary>
    public bool IsStop => Source is MovementSource.Walk && MovingTo is null;
}

/// <summary>
/// One movement of a floor item as the server reported it, including roller and wired moves that
/// leave the item where it was, which is how a wired cycle can be timed.
/// </summary>
/// <param name="ItemId">The item's room id.</param>
/// <param name="From">The tile it was on before.</param>
/// <param name="To">The tile it is on now.</param>
/// <param name="Direction">Its rotation after the movement.</param>
/// <param name="Source">What moved it.</param>
/// <param name="Duration">How long the client animates it, in milliseconds; 0 when the server does not say.</param>
/// <param name="Revision">The room revision the movement was applied at.</param>
/// <param name="Timestamp">When the movement was received, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
public sealed record FloorItemMovement(
    Id ItemId,
    Tile From,
    Tile To,
    int Direction,
    MovementSource Source,
    int Duration,
    long Revision,
    long Timestamp)
{
    /// <summary>Whether the item ended up on a different tile.</summary>
    public bool Relocated => From.XY != To.XY;
}
