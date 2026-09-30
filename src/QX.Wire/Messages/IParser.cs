namespace Qx.Messages;

/// <summary>Defines a type that can be read from a packet.</summary>
/// <typeparam name="T">The type that is read.</typeparam>
public interface IParser<T> where T : IParser<T>
{
    /// <summary>Reads a value from a packet.</summary>
    /// <param name="p">The reader to read from.</param>
    /// <returns>The value that was read.</returns>
    static abstract T Parse(in PacketReader p);
}
