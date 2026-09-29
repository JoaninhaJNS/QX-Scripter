using Qx.Platform;

namespace Qx.Scripting;

/// <summary>
/// The physical keyboard, read system-wide on Windows, macOS and X11 whichever window has focus.
/// Every handler is removed when the script stops.
/// </summary>
public sealed class ScriptKeyboard
{
    readonly Keyboard _keyboard;
    readonly Func<Action, Action> _guard;
    readonly Func<IDisposable, IDisposable> _track;

    internal ScriptKeyboard(Keyboard keyboard, Func<Action, Action> guard, Func<IDisposable, IDisposable> track)
    {
        _keyboard = keyboard;
        _guard = guard;
        _track = track;
    }

    /// <summary>Whether keys can be read on this system.</summary>
    public bool IsSupported => _keyboard.IsSupported;

    /// <summary>
    /// What the keyboard is read through, or why it cannot be read, such as a missing Input
    /// Monitoring permission on macOS or a Wayland session on Linux.
    /// </summary>
    public string Status => _keyboard.Status;

    /// <summary>Whether the key is held down at this moment; always <see langword="false"/> when unsupported.</summary>
    public bool IsDown(Key key) => _keyboard.IsDown(key);

    /// <summary>
    /// Runs the handler each time the key is pressed. Holding the key down does not repeat it,
    /// and a key already held when this is called is only reported on its next press.
    /// </summary>
    /// <returns>A handle that stops listening when disposed; also disposed when the script stops.</returns>
    public IDisposable OnDown(Key key, Action handler) => Watch(key, handler, true);

    /// <summary>Runs the handler each time the key is released.</summary>
    /// <returns>A handle that stops listening when disposed; also disposed when the script stops.</returns>
    public IDisposable OnUp(Key key, Action handler) => Watch(key, handler, false);

    IDisposable Watch(Key key, Action handler, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Action guarded = _guard(handler);
        return _track(_keyboard.Watch(key, down =>
        {
            if (down == pressed)
                guarded();
        }));
    }
}
