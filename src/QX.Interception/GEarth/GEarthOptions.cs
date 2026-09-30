namespace Qx.Interception.GEarth;

/// <summary>Represents the connection settings of a G-Earth extension and the info it reports to G-Earth.</summary>
public sealed class GEarthOptions
{
    /// <summary>Gets or sets the local G-Earth extension port to connect to, 9092 by default.</summary>
    /// <remarks>When <see cref="SearchPorts"/> is set, this is the first port tried. It must be between 1 and 65535.</remarks>
    public int Port { get; set; } = 9092;
    /// <summary>Gets or sets whether the following ports are tried when G-Earth does not answer on <see cref="Port"/>.</summary>
    public bool SearchPorts { get; set; }
    /// <summary>Gets or sets how many ports, starting at <see cref="Port"/>, are tried when <see cref="SearchPorts"/> is set.</summary>
    /// <remarks>Defaults to 32 and must be at least 1. The search never goes past port 65535.</remarks>
    public int PortSearchCount { get; set; } = 32;
    /// <summary>Gets or sets how long to wait for the first G-Earth control frame after connecting, 2 seconds by default.</summary>
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(2);
    /// <summary>Gets or sets the extension title shown in G-Earth.</summary>
    public string Title { get; set; } = "";
    /// <summary>Gets or sets the extension author shown in G-Earth.</summary>
    public string Author { get; set; } = "";
    /// <summary>Gets or sets the extension version shown in G-Earth, the product version by default.</summary>
    public string Version { get; set; } = Qx.ProductVersion.Current;
    /// <summary>Gets or sets the extension description shown in G-Earth.</summary>
    public string Description { get; set; } = "";
    /// <summary>Gets or sets whether the extension tells G-Earth that it reacts to being activated.</summary>
    public bool OnClickUsed { get; set; }
    /// <summary>Gets or sets whether the user may disconnect the extension from G-Earth, <see langword="true"/> by default.</summary>
    public bool CanLeave { get; set; } = true;
    /// <summary>Gets or sets whether the user may remove the extension from G-Earth, <see langword="true"/> by default.</summary>
    public bool CanDelete { get; set; } = true;
    /// <summary>Gets or sets the extension file path that G-Earth passed with <c>-f</c>.</summary>
    /// <remarks>G-Earth treats the extension as installed when this is not empty.</remarks>
    public string File { get; set; } = "";
    /// <summary>Gets or sets the authentication cookie that G-Earth passed with <c>-c</c>.</summary>
    public string Cookie { get; set; } = "";
    /// <summary>Gets whether G-Earth launched the process, which is the case when both <see cref="File"/> and <see cref="Cookie"/> are set.</summary>
    public bool IsLaunchedByGEarth =>
        !string.IsNullOrWhiteSpace(File) &&
        !string.IsNullOrWhiteSpace(Cookie);

    /// <summary>Reads the <c>-p</c>, <c>-f</c> and <c>-c</c> arguments that G-Earth passes to an extension.</summary>
    /// <remarks>Other arguments and a <c>-p</c> value that is not an integer are ignored.</remarks>
    /// <param name="args">The command line arguments.</param>
    /// <param name="baseOptions">The options to update in place, or <see langword="null"/> to start from defaults.</param>
    /// <returns><paramref name="baseOptions"/>, or a new instance, with the parsed values applied.</returns>
    public static GEarthOptions Parse(string[] args, GEarthOptions? baseOptions = null)
    {
        GEarthOptions options = baseOptions ?? new GEarthOptions();

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "-p" when int.TryParse(args[i + 1], out int port):
                    options.Port = port;
                    break;
                case "-f":
                    options.File = args[i + 1];
                    break;
                case "-c":
                    options.Cookie = args[i + 1];
                    break;
            }
        }

        return options;
    }
}
