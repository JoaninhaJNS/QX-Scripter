namespace Qx;

/// <summary>Provides the per-user directories the application stores its files in.</summary>
public static class StoragePaths
{
    /// <summary>Gets the application's configuration directory, <c>QX Scripter</c> under <see cref="ConfigurationHome"/>.</summary>
    public static string Configuration { get; } = Path.Combine(ConfigurationHome(), "QX Scripter");

    /// <summary>Gets the application's cache directory.</summary>
    /// <remarks>
    /// A <c>QX</c> folder inside the platform cache root: the local application data folder on Windows,
    /// <c>~/Library/Caches</c> on macOS and <c>$XDG_CACHE_HOME</c> or <c>~/.cache</c> on Linux.
    /// </remarks>
    public static string Cache { get; } = Path.Combine(CacheHome(), "QX");

    /// <summary>Gets the directory that holds saved scripts, <c>scripts</c> under <see cref="Configuration"/>.</summary>
    public static string Scripts => Path.Combine(Configuration, "scripts");

    /// <summary>Gets the string comparer that matches file paths the way the current platform does.</summary>
    /// <remarks>Ordinal and case-insensitive on Windows and macOS, ordinal and case-sensitive elsewhere.</remarks>
    public static StringComparer FileComparer => IgnoresPathCase
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>Gets the string comparison that matches file paths the way the current platform does.</summary>
    /// <remarks>Ordinal and case-insensitive on Windows and macOS, ordinal and case-sensitive elsewhere.</remarks>
    public static StringComparison FileComparison => IgnoresPathCase
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static bool IgnoresPathCase => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    /// <summary>Gets the platform's per-user configuration root.</summary>
    /// <remarks>
    /// The roaming application data folder on Windows, <c>~/Library/Application Support</c> on macOS and
    /// <c>$XDG_CONFIG_HOME</c> or <c>~/.config</c> on Linux. A relative XDG path is ignored.
    /// </remarks>
    /// <returns>The absolute directory path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user profile directory is needed but unavailable.</exception>
    public static string ConfigurationHome()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(UserHome(), "Library", "Application Support");
        if (OperatingSystem.IsLinux())
            return XdgDirectory("XDG_CONFIG_HOME", ".config");
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    private static string CacheHome()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(UserHome(), "Library", "Caches");
        if (OperatingSystem.IsLinux())
            return XdgDirectory("XDG_CACHE_HOME", ".cache");
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static string XdgDirectory(string variable, string fallback)
    {
        string? directory = Environment.GetEnvironmentVariable(variable);
        return !string.IsNullOrWhiteSpace(directory) && Path.IsPathFullyQualified(directory)
            ? Path.GetFullPath(directory) : Path.Combine(UserHome(), fallback);
    }

    private static string UserHome()
    {
        string directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("The user profile directory is unavailable.");
        return directory;
    }
}
