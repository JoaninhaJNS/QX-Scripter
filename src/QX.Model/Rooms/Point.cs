using Qx.Messages;

namespace Qx.Model;

/// <summary>Represents a pair of x and y tile coordinates.</summary>
/// <param name="X">The x coordinate.</param>
/// <param name="Y">The y coordinate.</param>
public readonly record struct Point(int X, int Y) : IParserComposer<Point>
{
    /// <summary>The point at (0, 0).</summary>
    public static readonly Point Zero = new(0, 0);

    /// <summary>Returns the coordinates as text.</summary>
    /// <returns>A string in the form <c>(X, Y)</c>.</returns>
    public override string ToString() => $"({X}, {Y})";

    /// <summary>Reads a point from a packet as two integers.</summary>
    /// <param name="p">The packet to read from.</param>
    public static Point Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt());

    /// <summary>Writes the point to a packet as two integers.</summary>
    /// <param name="p">The packet to write to.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(X);
        p.WriteInt(Y);
    }

    /// <summary>Gets the one-tile offset a direction points at.</summary>
    /// <remarks>
    /// Direction 0 is north (towards negative y), 2 east, 4 south and 6 west, with the odd values
    /// between them. Values outside 0 to 7 wrap around.
    /// </remarks>
    /// <param name="direction">The direction.</param>
    /// <returns>The offset, with each coordinate -1, 0 or 1.</returns>
    public static Point Offset(int direction) => (((direction % 8) + 8) % 8) switch
    {
        0 => new(0, -1),
        1 => new(1, -1),
        2 => new(1, 0),
        3 => new(1, 1),
        4 => new(0, 1),
        5 => new(-1, 1),
        6 => new(-1, 0),
        _ => new(-1, -1)
    };

    /// <summary>Gets the tile reached by going the given number of tiles in a direction.</summary>
    /// <param name="direction">The direction, as in <see cref="Offset"/>.</param>
    /// <param name="distance">The number of tiles to go.</param>
    /// <returns>The tile reached.</returns>
    public Point Step(int direction, int distance = 1)
    {
        Point offset = Offset(direction);
        return new(X + offset.X * distance, Y + offset.Y * distance);
    }

    /// <summary>Gets how many steps an avatar needs to reach another tile on an open floor.</summary>
    /// <remarks>A diagonal step counts as one.</remarks>
    /// <param name="other">The target tile.</param>
    /// <returns>The larger of the x and y distances.</returns>
    public int StepsTo(Point other) => Math.Max(Math.Abs(other.X - X), Math.Abs(other.Y - Y));

    /// <summary>Gets whether another tile touches this one, diagonals included.</summary>
    /// <param name="other">The other tile.</param>
    public bool IsNextTo(Point other) => StepsTo(other) == 1;

    /// <summary>Gets the direction from this tile towards another.</summary>
    /// <remarks>
    /// Only the signs of the x and y differences count, so any tile that differs on both axes gives
    /// a diagonal direction.
    /// </remarks>
    /// <param name="other">The target tile.</param>
    /// <returns>A direction from 0 to 7, or -1 when both are the same tile.</returns>
    public int DirectionTo(Point other) => (Math.Sign(other.X - X), Math.Sign(other.Y - Y)) switch
    {
        (0, -1) => 0,
        (1, -1) => 1,
        (1, 0) => 2,
        (1, 1) => 3,
        (0, 1) => 4,
        (-1, 1) => 5,
        (-1, 0) => 6,
        (-1, -1) => 7,
        _ => -1
    };

    /// <summary>Adds two points coordinate by coordinate.</summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The sum.</returns>
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    /// <summary>Subtracts one point from another coordinate by coordinate.</summary>
    /// <param name="a">The point to subtract from.</param>
    /// <param name="b">The point to subtract.</param>
    /// <returns>The difference.</returns>
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    /// <summary>Negates both coordinates of a point.</summary>
    /// <param name="p">The point to negate.</param>
    /// <returns>The negated point.</returns>
    public static Point operator -(Point p) => new(-p.X, -p.Y);

    /// <summary>Converts an x and y tuple to a <see cref="Point"/>.</summary>
    /// <param name="xy">The coordinates.</param>
    public static implicit operator Point((int X, int Y) xy) => new(xy.X, xy.Y);
}
