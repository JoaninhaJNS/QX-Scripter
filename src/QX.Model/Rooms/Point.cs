using Qx.Messages;

namespace Qx.Model;

public readonly record struct Point(int X, int Y) : IParserComposer<Point>
{
    public static readonly Point Zero = new(0, 0);

    public override string ToString() => $"({X}, {Y})";

    public static Point Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt());

    public void Compose(in PacketWriter p)
    {
        p.WriteInt(X);
        p.WriteInt(Y);
    }

    /// <summary>
    /// The one-tile offset a direction points at: 0 north (up the y axis), 2 east, 4 south,
    /// 6 west, and the odd values between them. Values outside 0-7 wrap around.
    /// </summary>
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

    /// <summary>The tile reached by going the given number of tiles in a direction.</summary>
    public Point Step(int direction, int distance = 1)
    {
        Point offset = Offset(direction);
        return new(X + offset.X * distance, Y + offset.Y * distance);
    }

    /// <summary>
    /// How many steps an avatar needs to get to another tile on an open floor, where a diagonal
    /// step counts as one.
    /// </summary>
    public int StepsTo(Point other) => Math.Max(Math.Abs(other.X - X), Math.Abs(other.Y - Y));

    /// <summary>Whether the other tile touches this one, diagonals included.</summary>
    public bool IsNextTo(Point other) => StepsTo(other) == 1;

    /// <summary>
    /// The direction from this tile towards another, rounded to the nearest of the eight, or -1
    /// when both are the same tile.
    /// </summary>
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

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator -(Point p) => new(-p.X, -p.Y);

    public static implicit operator Point((int X, int Y) xy) => new(xy.X, xy.Y);
}
