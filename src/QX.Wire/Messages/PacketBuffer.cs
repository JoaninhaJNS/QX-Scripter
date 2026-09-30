using System.Buffers;

namespace Qx.Messages;

/// <summary>Represents a growable byte buffer that holds a packet body in memory rented from the shared pool.</summary>
/// <remarks>Dispose the buffer to return its memory to the pool.</remarks>
/// <param name="minimumCapacity">The minimum initial capacity in bytes. The default is 32.</param>
public sealed class PacketBuffer(int minimumCapacity = PacketBuffer.InitialCapacity) : IDisposable
{
    const int InitialCapacity = 32;

    private volatile bool _disposed;
    private IMemoryOwner<byte> _owner = MemoryPool<byte>.Shared.Rent(minimumCapacity);

    /// <summary>Gets the number of bytes in use.</summary>
    public int Length { get; private set; }

    /// <summary>Gets the bytes in use, from offset 0 up to <see cref="Length"/>.</summary>
    public Span<byte> Span => _owner.Memory.Span[..Length];

    /// <summary>Initializes a new buffer that contains a copy of the specified bytes.</summary>
    /// <param name="data">The bytes to copy.</param>
    public PacketBuffer(ReadOnlySpan<byte> data)
        : this(data.Length)
    {
        Length = data.Length;
        data.CopyTo(Span);
    }

    /// <summary>Initializes a new buffer that contains a copy of the specified byte sequence.</summary>
    /// <param name="data">The bytes to copy.</param>
    public PacketBuffer(in ReadOnlySequence<byte> data)
        : this((int)data.Length)
    {
        Length = (int)data.Length;
        data.CopyTo(Span);
    }

    /// <summary>Grows the buffer so that its length is at least the specified number of bytes.</summary>
    /// <remarks>The capacity doubles until it fits. Bytes added past the old length are not zeroed.</remarks>
    /// <param name="min">The minimum length in bytes.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is negative.</exception>
    public void Grow(int min)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(min);

        if (_owner.Memory.Length < min)
        {
            int capacity = Math.Max(_owner.Memory.Length, InitialCapacity);
            while (capacity < min)
                capacity <<= 1;

            using IMemoryOwner<byte> old = _owner;
            _owner = MemoryPool<byte>.Shared.Rent(capacity);
            old.Memory.CopyTo(_owner.Memory);
        }

        if (Length < min)
            Length = min;
    }

    /// <summary>Gets a range of bytes at the specified offset, growing the buffer when the range ends past <see cref="Length"/>.</summary>
    /// <param name="start">The offset of the range, which must not be greater than <see cref="Length"/>.</param>
    /// <param name="length">The number of bytes in the range.</param>
    /// <returns>The bytes in the range. Existing bytes are kept and bytes past the old length are not zeroed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> or <paramref name="length"/> is negative, or <paramref name="start"/> is greater than <see cref="Length"/>.</exception>
    public Span<byte> Allocate(int start, int length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Length);

        Grow(start + length);
        return Span[start..(start + length)];
    }

    /// <summary>Resizes a range of bytes and moves the bytes after it to fit.</summary>
    /// <param name="range">The range to resize.</param>
    /// <param name="length">The new length of the range in bytes.</param>
    /// <returns>The resized range. Bytes past the old length of the range are not cleared.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative or <paramref name="range"/> is outside the bytes in use.</exception>
    public Span<byte> Resize(Range range, int length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var (start, preLen) = range.GetOffsetAndLength(Length);
        int diff = length - preLen;

        if (diff > 0)
        {
            Grow(Length + diff);
            Span[(start + preLen)..^diff].CopyTo(Span[(start + length)..]);
        }
        else if (diff < 0)
        {
            Span[(start + preLen)..].CopyTo(Span[(start + length)..^-diff]);
            Length += diff;
        }

        return Span[start..(start + length)];
    }

    /// <summary>Sets the length to 0 and keeps the rented memory.</summary>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer is disposed.</exception>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Length = 0;
    }

    /// <summary>Returns the rented memory to the pool; later calls do nothing.</summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _owner.Dispose();
    }

    /// <summary>Creates a new buffer that contains a copy of the bytes in use.</summary>
    /// <returns>The copied buffer.</returns>
    public PacketBuffer Copy() => new(Span);
}
