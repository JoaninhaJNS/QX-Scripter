using Qx.Platform;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    private static readonly Keyboard NoKeyboard = Qx.Platform.Keyboard.Unsupported("This host does not read the keyboard.");
    private ScriptKeyboard? _keyboard;

    /// <summary>
    /// The operating system QX runs on: <see cref="OsInfo.Kind"/>, <see cref="OsInfo.Version"/>,
    /// a readable <see cref="OsInfo.Name"/> such as "Windows 11 (build 26200)", the architecture
    /// and, on Linux, the display system.
    /// </summary>
    public OsInfo Os => OsInfo.Current;

    /// <summary>
    /// The physical keyboard, readable system-wide without any platform code in the script. Check
    /// <see cref="ScriptKeyboard.IsSupported"/> and <see cref="ScriptKeyboard.Status"/> first.
    /// </summary>
    public ScriptKeyboard Keyboard => _keyboard ??= new ScriptKeyboard(_hostKeyboard, Guarded, Track);
}
