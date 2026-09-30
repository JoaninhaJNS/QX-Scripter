using System.Collections;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>
/// Represents a read-only snapshot of items that can be filtered and sorted.
/// </summary>
/// <remarks>
/// The items are copied into an array when the query is created, so later changes to the source
/// sequence do not change the query. Filter and sort methods on derived queries return a new
/// query and leave the current one unchanged.
/// </remarks>
/// <typeparam name="T">The type of the items.</typeparam>
public abstract class QueryCollection<T> : IReadOnlyList<T>
{
    private readonly T[] _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCollection{T}"/> class with the specified items.
    /// </summary>
    /// <param name="items">The items to capture.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    protected QueryCollection(IEnumerable<T> items)
        : this(items, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCollection{T}"/> class with copies of the specified items.
    /// </summary>
    /// <param name="items">The items to capture.</param>
    /// <param name="snapshot">A function that copies each item, or <see langword="null"/> to capture the items as they are.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    protected QueryCollection(IEnumerable<T> items, Func<T, T>? snapshot)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = snapshot is null
            ? items.ToArray()
            : items.Select(snapshot).ToArray();
    }

    /// <summary>
    /// Gets the number of items in the query.
    /// </summary>
    public int Count => _items.Length;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
    public T this[int index] => _items[index];

    /// <summary>
    /// Gets the items in the query, in order.
    /// </summary>
    protected IEnumerable<T> Items => _items;

    /// <summary>
    /// Copies the items to a new array.
    /// </summary>
    /// <returns>A new array with the items in query order.</returns>
    public T[] ToArray() => [.. _items];

    /// <summary>
    /// Returns an enumerator that iterates through the items in order.
    /// </summary>
    /// <returns>An enumerator for the items.</returns>
    public IEnumerator<T> GetEnumerator() =>
        ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class QueryValues
{
    public static HashSet<T> Set<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.ToHashSet();
    }

    public static HashSet<string> Strings(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static void Epsilon(float epsilon)
    {
        if (!float.IsFinite(epsilon) || epsilon < 0)
            throw new ArgumentOutOfRangeException(nameof(epsilon), epsilon, "Epsilon must be finite and non-negative.");
    }

    public static bool Position(
        Tile location,
        int? x,
        int? y,
        float? z,
        int? direction,
        int actualDirection,
        float epsilon)
    {
        return
            (!x.HasValue || location.X == x.Value) &&
            (!y.HasValue || location.Y == y.Value) &&
            (!z.HasValue || Math.Abs(location.Z - z.Value) <= epsilon) &&
            (!direction.HasValue || actualDirection == direction.Value);
    }

    public static double DistanceSquared(Point first, Point second)
    {
        double dx = (double)first.X - second.X;
        double dy = (double)first.Y - second.Y;
        return dx * dx + dy * dy;
    }

    public static double DistanceSquared(Area area, Point point)
    {
        double dx = point.X < area.X1
            ? (double)area.X1 - point.X
            : point.X > area.X2
                ? (double)point.X - area.X2
                : 0d;
        double dy = point.Y < area.Y1
            ? (double)area.Y1 - point.Y
            : point.Y > area.Y2
                ? (double)point.Y - area.Y2
                : 0d;
        return dx * dx + dy * dy;
    }

    public static bool Adjacent(Area area, Point point, bool diagonals)
    {
        long dx = point.X < area.X1
            ? (long)area.X1 - point.X
            : point.X > area.X2
                ? (long)point.X - area.X2
                : 0;
        long dy = point.Y < area.Y1
            ? (long)area.Y1 - point.Y
            : point.Y > area.Y2
                ? (long)point.Y - area.Y2
                : 0;
        if (dx == 0 && dy == 0)
            return false;
        return diagonals ? Math.Max(dx, dy) == 1 : dx + dy == 1;
    }

    public static bool Adjacent(Area first, Area second, bool diagonals)
    {
        long dx = first.X2 < second.X1
            ? (long)second.X1 - first.X2
            : second.X2 < first.X1
                ? (long)first.X1 - second.X2
                : 0;
        long dy = first.Y2 < second.Y1
            ? (long)second.Y1 - first.Y2
            : second.Y2 < first.Y1
                ? (long)first.Y1 - second.Y2
                : 0;
        if (dx == 0 && dy == 0)
            return false;
        return diagonals ? Math.Max(dx, dy) == 1 : dx + dy == 1;
    }
}
