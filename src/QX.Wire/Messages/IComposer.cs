namespace Qx.Messages;

/// <summary>Defines a value that can write itself to a packet.</summary>
public interface IComposer
{
    /// <summary>Writes this value to a packet.</summary>
    /// <param name="p">The writer to write to.</param>
    void Compose(in PacketWriter p);
}
