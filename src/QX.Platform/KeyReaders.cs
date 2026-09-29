using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Qx.Platform;

interface IKeyReader : IDisposable
{
    bool IsSupported { get; }

    string Status { get; }

    void Refresh();

    bool IsDown(Key key);
}

static class KeyReaders
{
    public static IKeyReader Open()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return new WindowsKeyReader();
            if (OperatingSystem.IsMacOS())
                return new MacKeyReader();
            if (OperatingSystem.IsLinux())
                return X11KeyReader.Open();
            return new NoKeyReader($"Keys cannot be read on {OsInfo.Current.Name}.");
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return new NoKeyReader($"Keys cannot be read: {error.Message}");
        }
    }
}

sealed class NoKeyReader(string reason) : IKeyReader
{
    public bool IsSupported => false;

    public string Status { get; } = reason;

    public void Refresh()
    {
    }

    public bool IsDown(Key key) => false;

    public void Dispose()
    {
    }
}

[SupportedOSPlatform("windows")]
sealed partial class WindowsKeyReader : IKeyReader
{
    const short Pressed = unchecked((short)0x8000);

    public bool IsSupported => true;

    public string Status => "Windows";

    public void Refresh()
    {
    }

    public bool IsDown(Key key) => (GetAsyncKeyState(VirtualKey(key)) & Pressed) != 0;

    public void Dispose()
    {
    }

    static int VirtualKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => 0x41 + (key - Key.A),
        >= Key.D0 and <= Key.D9 => 0x30 + (key - Key.D0),
        >= Key.F1 and <= Key.F12 => 0x70 + (key - Key.F1),
        Key.Shift => 0x10,
        Key.Control => 0x11,
        Key.Alt => 0x12,
        Key.Space => 0x20,
        Key.Enter => 0x0D,
        Key.Escape => 0x1B,
        Key.Tab => 0x09,
        Key.Backspace => 0x08,
        Key.Left => 0x25,
        Key.Up => 0x26,
        Key.Right => 0x27,
        Key.Down => 0x28,
        Key.Insert => 0x2D,
        Key.Delete => 0x2E,
        Key.Home => 0x24,
        Key.End => 0x23,
        Key.PageUp => 0x21,
        Key.PageDown => 0x22,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown key.")
    };

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int virtual_key);
}

[SupportedOSPlatform("macos")]
sealed partial class MacKeyReader : IKeyReader
{
    const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    const int CombinedSessionState = 0;

    bool _requested;

    public bool IsSupported => true;

    public string Status => CGPreflightListenEventAccess()
        ? "macOS"
        : "macOS: allow QX under System Settings > Privacy & Security > Input Monitoring, then restart it";

    public void Refresh()
    {
        if (_requested)
            return;
        _requested = true;
        if (!CGPreflightListenEventAccess())
            CGRequestListenEventAccess();
    }

    public bool IsDown(Key key)
    {
        foreach (ushort code in Codes(key))
        {
            if (CGEventSourceKeyState(CombinedSessionState, code))
                return true;
        }
        return false;
    }

    public void Dispose()
    {
    }

    static ushort[] Codes(Key key) => key switch
    {
        Key.A => [0x00], Key.S => [0x01], Key.D => [0x02], Key.F => [0x03], Key.H => [0x04],
        Key.G => [0x05], Key.Z => [0x06], Key.X => [0x07], Key.C => [0x08], Key.V => [0x09],
        Key.B => [0x0B], Key.Q => [0x0C], Key.W => [0x0D], Key.E => [0x0E], Key.R => [0x0F],
        Key.Y => [0x10], Key.T => [0x11], Key.O => [0x1F], Key.U => [0x20], Key.I => [0x22],
        Key.P => [0x23], Key.L => [0x25], Key.J => [0x26], Key.K => [0x28], Key.N => [0x2D],
        Key.M => [0x2E],
        Key.D1 => [0x12], Key.D2 => [0x13], Key.D3 => [0x14], Key.D4 => [0x15], Key.D6 => [0x16],
        Key.D5 => [0x17], Key.D9 => [0x19], Key.D7 => [0x1A], Key.D8 => [0x1C], Key.D0 => [0x1D],
        Key.F1 => [0x7A], Key.F2 => [0x78], Key.F3 => [0x63], Key.F4 => [0x76], Key.F5 => [0x60],
        Key.F6 => [0x61], Key.F7 => [0x62], Key.F8 => [0x64], Key.F9 => [0x65], Key.F10 => [0x6D],
        Key.F11 => [0x67], Key.F12 => [0x6F],
        Key.Shift => [0x38, 0x3C],
        Key.Control => [0x3B, 0x3E],
        Key.Alt => [0x3A, 0x3D],
        Key.Space => [0x31],
        Key.Enter => [0x24, 0x4C],
        Key.Escape => [0x35],
        Key.Tab => [0x30],
        Key.Backspace => [0x33],
        Key.Left => [0x7B],
        Key.Right => [0x7C],
        Key.Down => [0x7D],
        Key.Up => [0x7E],
        Key.Insert => [0x72],
        Key.Delete => [0x75],
        Key.Home => [0x73],
        Key.End => [0x77],
        Key.PageUp => [0x74],
        Key.PageDown => [0x79],
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown key.")
    };

    [LibraryImport(CoreGraphics)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CGEventSourceKeyState(int state, ushort key);

    [LibraryImport(CoreGraphics)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CGPreflightListenEventAccess();

    [LibraryImport(CoreGraphics)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CGRequestListenEventAccess();
}

[SupportedOSPlatform("linux")]
sealed partial class X11KeyReader : IKeyReader
{
    const string Xlib = "libX11.so.6";

    readonly byte[] _keymap = new byte[32];
    readonly Dictionary<nuint, byte> _codes = [];
    readonly string _status;
    nint _display;

    X11KeyReader(nint display, string status)
    {
        _display = display;
        _status = status;
    }

    public static IKeyReader Open()
    {
        DisplayServer display = OsInfo.Current.Display;
        if (display is DisplayServer.Wayland)
            return new NoKeyReader("Keys cannot be read in a Wayland session without XWayland.");
        if (display is DisplayServer.None)
            return new NoKeyReader("Keys cannot be read without a graphical session.");
        nint handle = XOpenDisplay(0);
        if (handle == 0)
            return new NoKeyReader("Keys cannot be read: the X11 display could not be opened.");
        return new X11KeyReader(handle, display is DisplayServer.XWayland
            ? "X11 through XWayland: keys are only seen while an X11 window has focus"
            : "X11");
    }

    public bool IsSupported => _display != 0;

    public string Status => _status;

    public void Refresh()
    {
        ObjectDisposedException.ThrowIf(_display == 0, this);
        XQueryKeymap(_display, _keymap);
    }

    public bool IsDown(Key key)
    {
        foreach (nuint symbol in Symbols(key))
        {
            if (!_codes.TryGetValue(symbol, out byte code))
            {
                code = XKeysymToKeycode(_display, symbol);
                _codes[symbol] = code;
            }
            if (code != 0 && (_keymap[code >> 3] & (1 << (code & 7))) != 0)
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_display == 0)
            return;
        XCloseDisplay(_display);
        _display = 0;
    }

    static nuint[] Symbols(Key key) => key switch
    {
        >= Key.A and <= Key.Z => [(nuint)('a' + (key - Key.A))],
        >= Key.D0 and <= Key.D9 => [(nuint)('0' + (key - Key.D0))],
        >= Key.F1 and <= Key.F12 => [(nuint)(0xFFBE + (key - Key.F1))],
        Key.Shift => [0xFFE1, 0xFFE2],
        Key.Control => [0xFFE3, 0xFFE4],
        Key.Alt => [0xFFE9, 0xFFEA],
        Key.Space => [0x20],
        Key.Enter => [0xFF0D, 0xFF8D],
        Key.Escape => [0xFF1B],
        Key.Tab => [0xFF09],
        Key.Backspace => [0xFF08],
        Key.Left => [0xFF51],
        Key.Up => [0xFF52],
        Key.Right => [0xFF53],
        Key.Down => [0xFF54],
        Key.Insert => [0xFF63],
        Key.Delete => [0xFFFF],
        Key.Home => [0xFF50],
        Key.End => [0xFF57],
        Key.PageUp => [0xFF55],
        Key.PageDown => [0xFF56],
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown key.")
    };

    [LibraryImport(Xlib)]
    private static partial nint XOpenDisplay(nint name);

    [LibraryImport(Xlib)]
    private static partial int XCloseDisplay(nint display);

    [LibraryImport(Xlib)]
    private static partial int XQueryKeymap(nint display, [Out] byte[] keys);

    [LibraryImport(Xlib)]
    private static partial byte XKeysymToKeycode(nint display, nuint keysym);
}
