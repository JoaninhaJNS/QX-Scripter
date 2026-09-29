using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Qx.Platform;

/// <summary>The operating system family QX runs on.</summary>
public enum OsKind
{
    Windows,
    MacOS,
    Linux,
    Other
}

/// <summary>How a Linux desktop session draws its windows, which decides what QX can read from it.</summary>
public enum DisplayServer
{
    /// <summary>Windows and macOS, which have one native display system.</summary>
    Native,

    /// <summary>An X11 session.</summary>
    X11,

    /// <summary>A Wayland session with XWayland available for X11 programs.</summary>
    XWayland,

    /// <summary>A Wayland session without X11.</summary>
    Wayland,

    /// <summary>No graphical session, such as a server or a plain terminal.</summary>
    None
}

/// <summary>The operating system QX runs on: its family, version, name and display system.</summary>
public sealed class OsInfo
{
    OsInfo(OsKind kind, Version version, string name, DisplayServer display)
    {
        Kind = kind;
        Version = version;
        Name = name;
        Display = display;
    }

    /// <summary>The system QX is running on.</summary>
    public static OsInfo Current { get; } = Detect();

    /// <summary>The operating system family.</summary>
    public OsKind Kind { get; }

    public bool IsWindows => Kind is OsKind.Windows;

    public bool IsMacOS => Kind is OsKind.MacOS;

    public bool IsLinux => Kind is OsKind.Linux;

    /// <summary>
    /// The version of the system itself: the Windows build such as 10.0.26200, the macOS release
    /// such as 15.2, or the distribution release on Linux, falling back to the kernel version.
    /// </summary>
    public Version Version { get; }

    /// <summary>A readable name such as "Windows 11 (build 26200)", "macOS 15.2" or "Ubuntu 24.04 LTS".</summary>
    public string Name { get; }

    /// <summary>The processor architecture of the system, such as "x64" or "arm64".</summary>
    public string Architecture => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

    /// <summary>The display system of the session.</summary>
    public DisplayServer Display { get; }

    /// <summary>The raw description the runtime reports.</summary>
    public string Description => RuntimeInformation.OSDescription;

    public override string ToString() => $"{Name} {Architecture}";

    static OsInfo Detect()
    {
        if (OperatingSystem.IsWindows())
            return Windows();
        if (OperatingSystem.IsMacOS())
            return new OsInfo(OsKind.MacOS, Environment.OSVersion.Version, $"macOS {Readable(Environment.OSVersion.Version)}", DisplayServer.Native);
        if (OperatingSystem.IsLinux())
            return Linux();
        return new OsInfo(OsKind.Other, Environment.OSVersion.Version, RuntimeInformation.OSDescription, DisplayServer.None);
    }

    static OsInfo Windows()
    {
        Version version = Environment.OSVersion.Version;
        string release = version switch
        {
            { Major: 10, Build: >= 22000 } => "Windows 11",
            { Major: 10 } => "Windows 10",
            _ => $"Windows {version.Major}.{version.Minor}"
        };
        return new OsInfo(OsKind.Windows, version, $"{release} (build {version.Build})", DisplayServer.Native);
    }

    static OsInfo Linux()
    {
        IReadOnlyDictionary<string, string> release = OsRelease();
        Version version = release.TryGetValue("VERSION_ID", out string? id) && TryVersion(id, out Version? parsed)
            ? parsed
            : Environment.OSVersion.Version;
        string name = release.TryGetValue("PRETTY_NAME", out string? pretty) && pretty.Length > 0
            ? pretty
            : release.TryGetValue("NAME", out string? plain) && plain.Length > 0
                ? plain
                : $"Linux {Readable(Environment.OSVersion.Version)}";
        return new OsInfo(OsKind.Linux, version, name, LinuxDisplay());
    }

    static DisplayServer LinuxDisplay()
    {
        bool x11 = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
        bool wayland = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
        return (x11, wayland) switch
        {
            (true, true) => DisplayServer.XWayland,
            (true, false) => DisplayServer.X11,
            (false, true) => DisplayServer.Wayland,
            _ => DisplayServer.None
        };
    }

    static IReadOnlyDictionary<string, string> OsRelease()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in (string[])["/etc/os-release", "/usr/lib/os-release"])
        {
            try
            {
                if (!File.Exists(path))
                    continue;
                foreach (string line in File.ReadLines(path))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0 || line.StartsWith('#'))
                        continue;
                    values[line[..separator].Trim()] = line[(separator + 1)..].Trim().Trim('"', '\'');
                }
                return values;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
            }
        }
        return values;
    }

    static bool TryVersion(string text, [NotNullWhen(true)] out Version? version)
    {
        string[] parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var numbers = new int[Math.Max(2, Math.Min(parts.Length, 4))];
        for (int i = 0; i < parts.Length && i < numbers.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
            {
                version = null;
                return false;
            }
        }
        version = numbers.Length switch
        {
            2 => new Version(numbers[0], numbers[1]),
            3 => new Version(numbers[0], numbers[1], numbers[2]),
            _ => new Version(numbers[0], numbers[1], numbers[2], numbers[3])
        };
        return true;
    }

    static string Readable(Version version) =>
        version.Build > 0 ? $"{version.Major}.{version.Minor}.{version.Build}" : $"{version.Major}.{version.Minor}";
}
