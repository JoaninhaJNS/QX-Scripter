using Qx.Model;

namespace Qx.Game;

/// <summary>Specifies what moved an avatar or a floor item.</summary>
public enum MovementSource
{
    /// <summary>A status update in which the avatar walked, stopped or had its position restated.</summary>
    Walk,

    /// <summary>A roller that slid the avatar or the item to the next tile.</summary>
    Roller,

    /// <summary>A wired effect that moved, teleported or rotated the avatar or the item.</summary>
    Wired,

    /// <summary>A move or rotation of the item by a user with rights.</summary>
    Update
}

/// <summary>Represents one movement of an avatar as the server reported it.</summary>
/// <remarks>
/// Unlike a change of the avatar's location, a movement is reported for every status update, so a
/// walk that starts, continues or stops on the same tile is still observed, together with its cause.
/// </remarks>
/// <param name="Index">The avatar's room index.</param>
/// <param name="IsSelf">Whether the avatar is the user's own.</param>
/// <param name="From">The tile the avatar was on before this movement.</param>
/// <param name="To">The tile the avatar is on now.</param>
/// <param name="MovingTo">
/// For <see cref="MovementSource.Walk"/>, the tile the avatar is stepping onto next, or
/// <see langword="null"/> when it stands still. Always <see langword="null"/> for rollers and wired.
/// </param>
/// <param name="Source">What caused the movement.</param>
/// <param name="Duration">The time the client animates the movement, in milliseconds, or 0 when the server does not send one.</param>
/// <param name="Revision">
/// The room revision the movement was applied at. Compare it with <see cref="RoomManager.Revision"/>
/// read before sending a command to tell movements caused by that command from older ones.
/// </param>
/// <param name="Timestamp">The time the movement was received, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
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
    /// <summary>Gets whether the avatar ended up on a different tile.</summary>
    public bool Relocated => From.XY != To.XY;

    /// <summary>Gets whether the movement is a walk update that shows the avatar standing still.</summary>
    public bool IsStop => Source is MovementSource.Walk && MovingTo is null;
}

/// <summary>Represents one movement of a floor item as the server reported it.</summary>
/// <remarks>
/// Roller and wired moves that leave the item on the same tile are included, which makes it
/// possible to time a wired cycle.
/// </remarks>
/// <param name="ItemId">The item's room id.</param>
/// <param name="From">The tile the item was on before the movement.</param>
/// <param name="To">The tile the item is on now.</param>
/// <param name="Direction">The item's rotation after the movement.</param>
/// <param name="Source">What moved the item.</param>
/// <param name="Duration">The time the client animates the movement, in milliseconds, or 0 when the server does not send one.</param>
/// <param name="Revision">The room revision the movement was applied at.</param>
/// <param name="Timestamp">The time the movement was received, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
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
    /// <summary>Gets whether the item ended up on a different tile.</summary>
    public bool Relocated => From.XY != To.XY;
}
