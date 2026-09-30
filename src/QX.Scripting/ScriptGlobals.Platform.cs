using Qx.Platform;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    private static readonly Keyboard NoKeyboard = Qx.Platform.Keyboard.Unsupported("This host does not read the keyboard.");
    private ScriptKeyboard? _keyboard;

    /// <summary>
    /// Gets the operating system QX runs on.
    /// </summary>
    /// <remarks>
    /// It reports <see cref="OsInfo.Kind"/>, <see cref="OsInfo.Version"/>, a readable
    /// <see cref="OsInfo.Name"/> such as "Windows 11 (build 26200)", the
    /// <see cref="OsInfo.Architecture"/> and, on Linux, the <see cref="OsInfo.Display"/> system.
    /// </remarks>
    public OsInfo Os => OsInfo.Current;

    /// <summary>
    /// Gets the physical keyboard, readable system-wide without any platform code in the script.
    /// </summary>
    /// <remarks>
    /// Check <see cref="ScriptKeyboard.IsSupported"/> and <see cref="ScriptKeyboard.Status"/>
    /// first. When the host provides no keyboard, it reports itself as unsupported.
    /// </remarks>
    public ScriptKeyboard Keyboard => _keyboard ??= new ScriptKeyboard(_hostKeyboard, Guarded, Track);
}
