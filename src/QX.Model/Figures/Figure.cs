using System.Collections.ObjectModel;
using System.Globalization;

namespace Qx.Model.Figures;

/// <summary>
/// Represents an avatar figure as an ordered list of figure parts.
/// </summary>
/// <remarks>
/// The string form is a list of parts separated by <c>.</c>, such as <c>hr-115-42.hd-195-19</c>.
/// Two figures are equal when they contain equal parts in the same order.
/// </remarks>
public sealed class Figure : IEquatable<Figure>
{
    private readonly FigurePart[] _parts;
    private readonly ReadOnlyCollection<FigurePart> _readOnlyParts;

    /// <summary>Gets the empty figure, which has no parts.</summary>
    public static Figure Empty { get; } = new([]);

    /// <summary>Gets the parts of the figure in figure string order.</summary>
    public IReadOnlyList<FigurePart> Parts => _readOnlyParts;

    /// <summary>Initializes a new instance of the <see cref="Figure"/> class with the specified parts.</summary>
    /// <param name="parts">The parts of the figure, in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parts"/> contains a <see langword="null"/> part.</exception>
    public Figure(IEnumerable<FigurePart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        _parts = parts.ToArray();

        if (_parts.Any(static part => part is null))
            throw new ArgumentException("Figure parts cannot contain null values.", nameof(parts));

        _readOnlyParts = Array.AsReadOnly(_parts);
    }

    /// <summary>Parses a figure string such as <c>hr-115-42.hd-195-19</c>.</summary>
    /// <param name="value">The figure string. An empty string yields <see cref="Empty"/>.</param>
    /// <returns>The parsed figure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when a part has no set id, or its type, set id or a color id is invalid.</exception>
    public static Figure Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!try_parse(value, out Figure? figure, out string? error))
            throw new FormatException(error);
        return figure;
    }

    /// <summary>Tries to parse a figure string such as <c>hr-115-42.hd-195-19</c>.</summary>
    /// <param name="value">The figure string. An empty string yields <see cref="Empty"/>.</param>
    /// <param name="figure">The parsed figure, or <see cref="Empty"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if the string was parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out Figure figure)
    {
        if (value is null || !try_parse(value, out Figure? parsed, out _))
        {
            figure = Empty;
            return false;
        }

        figure = parsed;
        return true;
    }

    /// <summary>Gets the distinct part types in the figure, in order of first occurrence.</summary>
    public IReadOnlyList<FigurePartType> PartTypes
    {
        get
        {
            List<FigurePartType> types = [];
            HashSet<FigurePartType> seen = [];

            foreach (FigurePart part in _parts)
            {
                if (seen.Add(part.Type))
                    types.Add(part.Type);
            }

            return types.AsReadOnly();
        }
    }

    /// <summary>Gets whether the figure contains a part of the specified type.</summary>
    /// <param name="type">The part type to look for.</param>
    public bool HasPartType(FigurePartType type) => _parts.Any(part => part.Type == type);

    /// <summary>Gets every part of the specified type, in figure order.</summary>
    /// <param name="type">The part type to look for.</param>
    /// <returns>The matching parts, empty when the figure has none.</returns>
    public IReadOnlyList<FigurePart> FindParts(FigurePartType type) =>
        Array.AsReadOnly(_parts.Where(part => part.Type == type).ToArray());

    /// <summary>Gets the last part of the specified type, which is the one the client renders.</summary>
    /// <param name="type">The part type to look for.</param>
    /// <returns>The last matching part, or <see langword="null"/> when the figure has none.</returns>
    public FigurePart? FindLastPart(FigurePartType type)
    {
        for (int index = _parts.Length - 1; index >= 0; index--)
        {
            if (_parts[index].Type == type)
                return _parts[index];
        }

        return null;
    }

    /// <summary>Returns a new figure with the part appended, keeping any existing part of the same type.</summary>
    /// <param name="part">The part to append.</param>
    /// <returns>A new figure that ends with <paramref name="part"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="part"/> is <see langword="null"/>.</exception>
    public Figure Add(FigurePart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        return new Figure(_parts.Append(part));
    }

    /// <summary>Returns a new figure where the part replaces every existing part of the same type.</summary>
    /// <param name="part">The part to set. It is appended after the remaining parts.</param>
    /// <returns>A new figure with <paramref name="part"/> as the only part of its type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="part"/> is <see langword="null"/>.</exception>
    public Figure SetPart(FigurePart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        return new Figure(_parts.Where(existing => existing.Type != part.Type).Append(part));
    }

    /// <summary>Returns a new figure without any part of the specified type.</summary>
    /// <param name="type">The part type to remove.</param>
    /// <returns>A new figure without parts of <paramref name="type"/>.</returns>
    public Figure RemoveParts(FigurePartType type) =>
        new(_parts.Where(part => part.Type != type));

    /// <summary>
    /// Collapses repeated part types the way the client's figure container does: the last
    /// occurrence of a type wins and takes the position of that last occurrence.
    /// </summary>
    /// <returns>The collapsed figure, or the same instance when no part type repeats.</returns>
    public Figure Normalize()
    {
        List<FigurePart> parts = [];

        foreach (FigurePart part in _parts)
        {
            parts.RemoveAll(existing => existing.Type == part.Type);
            parts.Add(part);
        }

        return parts.Count == _parts.Length ? this : new Figure(parts);
    }

    /// <summary>Returns the figure string, with the parts joined by <c>.</c>.</summary>
    /// <returns>The figure string, empty for a figure without parts.</returns>
    public override string ToString() => string.Join(".", _parts.Select(static part => part.ToString()));

    /// <summary>Gets whether another figure contains equal parts in the same order.</summary>
    /// <param name="other">The figure to compare with.</param>
    public bool Equals(Figure? other) =>
        other is not null &&
        _parts.AsSpan().SequenceEqual(other._parts);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Figure other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (FigurePart part in _parts)
            hash.Add(part);
        return hash.ToHashCode();
    }

    private static bool try_parse(string value, out Figure figure, out string? error)
    {
        if (value.Length == 0)
        {
            figure = Empty;
            error = null;
            return true;
        }

        string[] rawParts = value.Split('.');
        FigurePart[] parts = new FigurePart[rawParts.Length];

        for (int partIndex = 0; partIndex < rawParts.Length; partIndex++)
        {
            string rawPart = rawParts[partIndex];
            string[] tokens = rawPart.Split('-');

            if (tokens.Length < 2)
            {
                figure = Empty;
                error = $"Figure part {partIndex} must contain a type and set ID.";
                return false;
            }

            if (!FigurePartType.TryParse(tokens[0], out FigurePartType type))
            {
                figure = Empty;
                error = $"Figure part {partIndex} has an invalid type.";
                return false;
            }

            if (!try_parse_id(tokens[1], out int setId))
            {
                figure = Empty;
                error = $"Figure part {partIndex} has an invalid set ID.";
                return false;
            }

            int[] colorIds = new int[tokens.Length - 2];
            for (int colorIndex = 0; colorIndex < colorIds.Length; colorIndex++)
            {
                if (!try_parse_id(tokens[colorIndex + 2], out colorIds[colorIndex]))
                {
                    figure = Empty;
                    error = $"Figure part {partIndex} has an invalid color ID at index {colorIndex}.";
                    return false;
                }
            }

            parts[partIndex] = new FigurePart(type, setId, colorIds);
        }

        figure = new Figure(parts);
        error = null;
        return true;
    }

    private static bool try_parse_id(string value, out int id)
    {
        if (value.Length > 0 &&
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
        {
            return true;
        }

        id = 0;
        return false;
    }
}
