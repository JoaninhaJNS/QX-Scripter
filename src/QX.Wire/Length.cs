namespace Qx;

/// <summary>Represents the element count that prefixes an array in a packet.</summary>
/// <remarks>
/// The value is an unsigned 16-bit integer, from 0 to 65535. Flash sends lengths as 32-bit
/// big-endian integers.
/// </remarks>
public readonly record struct Length : IComparable<Length>, IComparable
{
    private readonly ushort _value;
    private Length(ushort value) => _value = value;

    /// <summary>Converts a length to its <see cref="ushort"/> value.</summary>
    /// <param name="length">The length to convert.</param>
    public static implicit operator ushort(Length length) => length._value;
    /// <summary>Converts a <see cref="ushort"/> to a length.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Length(ushort value) => new(value);
    /// <summary>Converts an <see cref="int"/> to a length.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">Thrown when <paramref name="value"/> is negative or greater than 65535.</exception>
    public static explicit operator Length(int value) => new(checked((ushort)value));

    /// <summary>Returns the length as a decimal string.</summary>
    public override string ToString() => _value.ToString();

    /// <summary>Compares this length with another length by value.</summary>
    /// <param name="other">The length to compare with.</param>
    /// <returns>A negative number, zero or a positive number when this length is less than, equal to or greater than <paramref name="other"/>.</returns>
    public int CompareTo(Length other) => _value.CompareTo(other._value);

    /// <summary>Compares this length with an object that must be a <see cref="Length"/>.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A negative number, zero or a positive number when this length is less than, equal to or greater than <paramref name="obj"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not a <see cref="Length"/>.</exception>
    public int CompareTo(object? obj)
    {
        if (obj is not Length other)
            throw new ArgumentException($"Object must be of type {typeof(Length).FullName}.", nameof(obj));
        return CompareTo(other);
    }
}
