using System.Buffers.Binary;

namespace Qx.Interception.GEarth;

/// <summary>Provides access to the parts of a complete G-Earth control frame.</summary>
/// <remarks>A frame is a big-endian 32-bit length that counts the header and body, a big-endian 16-bit header and the body.</remarks>
public static class GControlFrame
{
    /// <summary>Gets the length declared in the first four bytes of the frame.</summary>
    /// <param name="frame">The complete frame.</param>
    /// <returns>The byte count of the header and body.</returns>
    public static int DeclaredLength(ReadOnlySpan<byte> frame) => BinaryPrimitives.ReadInt32BigEndian(frame[..4]);

    /// <summary>Gets the control header stored after the length prefix.</summary>
    /// <param name="frame">The complete frame.</param>
    /// <returns>The control header, one of the <see cref="GControl"/> constants.</returns>
    public static short Header(ReadOnlySpan<byte> frame) => BinaryPrimitives.ReadInt16BigEndian(frame.Slice(4, 2));

    /// <summary>Gets the frame body that follows the six-byte length and header.</summary>
    /// <param name="frame">The complete frame.</param>
    /// <returns>A slice of <paramref name="frame"/>.</returns>
    public static ReadOnlySpan<byte> Body(ReadOnlySpan<byte> frame) => frame[6..];
}
