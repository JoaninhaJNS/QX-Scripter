using System.Reflection;

namespace Qx;

/// <summary>Provides the running product version.</summary>
public static class ProductVersion
{
    /// <summary>Gets the product version of the entry assembly.</summary>
    /// <remarks>
    /// Read from the entry assembly, or from the core assembly when there is none. Uses the informational
    /// version with any <c>+</c> build metadata removed, then the three-part assembly version, then <c>0.0.0</c>.
    /// </remarks>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(ProductVersion).Assembly;
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return WithoutMetadata(informational);

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static string WithoutMetadata(string version)
    {
        int metadata = version.IndexOf('+');
        return metadata > 0 ? version[..metadata] : version;
    }
}
