namespace Qx.Model;

/// <summary>Represents one tile of the live heightmap.</summary>
/// <param name="X">The tile x coordinate.</param>
/// <param name="Y">The tile y coordinate.</param>
/// <param name="Value">
/// The raw value. Negative means the tile is not floor; otherwise bit 14 (0x4000) is the blocked
/// flag and the low 14 bits are the height in 1/256 tile units.
/// </param>
public readonly record struct HeightmapTile(int X, int Y, short Value)
{
    /// <summary>Gets whether the tile is floor, which is when <see cref="Value"/> is not negative.</summary>
    public bool IsFloor => Value >= 0;
    /// <summary>Gets whether the blocked bit (0x4000) of <see cref="Value"/> is set.</summary>
    public bool IsBlocked => (Value & 0x4000) != 0;
    /// <summary>Gets whether the tile is floor and not blocked.</summary>
    public bool IsFree => IsFloor && !IsBlocked;
    /// <summary>Gets the stack height in tile units, or -1 when the tile is not floor.</summary>
    public double Height => Value >= 0 ? (Value & 0x3FFF) / 256.0 : -1;
    /// <summary>Gets the tile coordinates.</summary>
    public Point Location => new(X, Y);
}
