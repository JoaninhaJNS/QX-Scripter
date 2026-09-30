namespace Qx.Interception.GEarth;

/// <summary>Represents the information G-Earth reports about itself to an extension.</summary>
public sealed class HostInfo
{
    /// <summary>Gets or sets the packet logger that G-Earth reports.</summary>
    public string PacketLogger { get; set; } = "";
    /// <summary>Gets or sets the G-Earth version.</summary>
    public string Version { get; set; } = "";
    /// <summary>Gets the additional attributes, keyed case-insensitively.</summary>
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reads host information from a control frame body.</summary>
    /// <remarks>
    /// Reads the packet logger and version strings, then a 32-bit attribute count followed by that many
    /// key and value strings. A repeated key keeps its last value.
    /// </remarks>
    /// <param name="reader">The reader positioned at the host information, which is advanced past it.</param>
    /// <returns>The host information read.</returns>
    public static HostInfo Read(ref GControlReader reader)
    {
        var info = new HostInfo
        {
            PacketLogger = reader.ReadString(),
            Version = reader.ReadString()
        };

        int count = reader.ReadInt();
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            string value = reader.ReadString();
            info.Attributes[key] = value;
        }

        return info;
    }
}
