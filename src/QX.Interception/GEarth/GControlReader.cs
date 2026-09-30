using System.Buffers.Binary;
using System.Text;

namespace Qx.Interception.GEarth;

/// <summary>Represents a forward-only reader over the body of a G-Earth control frame.</summary>
/// <remarks>Numbers are big-endian. Reading past the end of the body throws.</remarks>
/// <param name="body">The frame body to read.</param>
public ref struct GControlReader(ReadOnlySpan<byte> body)
{
    private readonly ReadOnlySpan<byte> _span = body;
    private int _pos = 0;

    /// <summary>Gets the current read offset in bytes.</summary>
    public readonly int Position => _pos;
    /// <summary>Gets the number of bytes left to read.</summary>
    public readonly int Available => _span.Length - _pos;
    /// <summary>Gets whether every byte of the body has been read.</summary>
    public readonly bool IsEof => _pos >= _span.Length;

    /// <summary>Reads one byte.</summary>
    /// <returns>The byte read.</returns>
    public byte ReadByte() => _span[_pos++];

    /// <summary>Reads one byte as a boolean, where any nonzero value is <see langword="true"/>.</summary>
    /// <returns>The value read.</returns>
    public bool ReadBool() => _span[_pos++] != 0;

    /// <summary>Reads a 16-bit integer.</summary>
    /// <returns>The value read.</returns>
    public short ReadShort()
    {
        short value = BinaryPrimitives.ReadInt16BigEndian(_span.Slice(_pos, 2));
        _pos += 2;
        return value;
    }

    /// <summary>Reads a 32-bit integer.</summary>
    /// <returns>The value read.</returns>
    public int ReadInt()
    {
        int value = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_pos, 4));
        _pos += 4;
        return value;
    }

    /// <summary>Reads a number of raw bytes.</summary>
    /// <param name="n">The number of bytes to read.</param>
    /// <returns>A slice of the body, not a copy.</returns>
    public ReadOnlySpan<byte> ReadBytes(int n)
    {
        ReadOnlySpan<byte> slice = _span.Slice(_pos, n);
        _pos += n;
        return slice;
    }

    /// <summary>Reads a Latin-1 string prefixed with an unsigned 16-bit byte length.</summary>
    /// <returns>The string read.</returns>
    public string ReadString()
    {
        int len = BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(_pos, 2));
        _pos += 2;
        string value = Encoding.Latin1.GetString(_span.Slice(_pos, len));
        _pos += len;
        return value;
    }

    /// <summary>Reads a Latin-1 string prefixed with a 32-bit byte length.</summary>
    /// <returns>The string read.</returns>
    public string ReadLongString()
    {
        int len = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_pos, 4));
        _pos += 4;
        string value = Encoding.Latin1.GetString(_span.Slice(_pos, len));
        _pos += len;
        return value;
    }

    /// <summary>Reads a UTF-8 string prefixed with a 32-bit byte length.</summary>
    /// <returns>The string read.</returns>
    public string ReadLongStringUtf8()
    {
        int len = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_pos, 4));
        _pos += 4;
        string value = Encoding.UTF8.GetString(_span.Slice(_pos, len));
        _pos += len;
        return value;
    }
}
