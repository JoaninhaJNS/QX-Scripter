using System.Collections.ObjectModel;
using System.Globalization;

namespace Qx.Model.Figures;

/// <summary>
/// Represents one part of a figure, a part type with a set id and color ids.
/// </summary>
/// <remarks>
/// The string form is <c>type-set</c> followed by <c>-color</c> for each color id, such as <c>hr-115-42</c>.
/// </remarks>
public sealed class FigurePart : IEquatable<FigurePart>
{
    private readonly int[] _colorIds;
    private readonly ReadOnlyCollection<int> _colors;

    /// <summary>Gets the part type.</summary>
    public FigurePartType Type { get; }
    /// <summary>Gets the id of the figure data set the part selects.</summary>
    public int SetId { get; }
    /// <summary>Gets the palette color ids of the part, in order, empty when the part has none.</summary>
    public IReadOnlyList<int> ColorIds => _colors;

    /// <summary>Initializes a new instance of the <see cref="FigurePart"/> class.</summary>
    /// <param name="type">The part type.</param>
    /// <param name="setId">The id of the figure data set the part selects.</param>
    /// <param name="colorIds">The palette color ids, or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is the default value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="setId"/> or a color id is negative.</exception>
    public FigurePart(FigurePartType type, int setId, IEnumerable<int>? colorIds = null)
    {
        if (type == default)
            throw new ArgumentException("A figure part type is required.", nameof(type));
        if (setId < 0)
            throw new ArgumentOutOfRangeException(nameof(setId));

        Type = type;
        SetId = setId;
        _colorIds = colorIds?.ToArray() ?? [];

        if (_colorIds.Any(static colorId => colorId < 0))
            throw new ArgumentOutOfRangeException(nameof(colorIds), "Figure color IDs cannot be negative.");

        _colors = Array.AsReadOnly(_colorIds);
    }

    /// <summary>Returns the part in figure string form, such as <c>hr-115-42</c>.</summary>
    /// <returns>The part string.</returns>
    public override string ToString()
    {
        string value = string.Concat(Type.Value, "-", SetId.ToString(CultureInfo.InvariantCulture));
        return _colorIds.Length == 0
            ? value
            : string.Concat(value, "-", string.Join("-", _colorIds));
    }

    /// <summary>Gets whether another part has the same type, set id and color ids in the same order.</summary>
    /// <param name="other">The part to compare with.</param>
    public bool Equals(FigurePart? other) =>
        other is not null &&
        Type == other.Type &&
        SetId == other.SetId &&
        _colorIds.AsSpan().SequenceEqual(other._colorIds);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FigurePart other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Type);
        hash.Add(SetId);
        foreach (int colorId in _colorIds)
            hash.Add(colorId);
        return hash.ToHashCode();
    }
}
