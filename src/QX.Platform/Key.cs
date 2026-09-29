namespace Qx.Platform;

/// <summary>A key on the keyboard, independent of the operating system that reads it.</summary>
public enum Key
{
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    /// <summary>Either shift key.</summary>
    Shift,

    /// <summary>Either control key.</summary>
    Control,

    /// <summary>Either alt key; option on macOS.</summary>
    Alt,

    Space,
    Enter,
    Escape,
    Tab,
    Backspace,
    Left,
    Up,
    Right,
    Down,
    Insert,
    Delete,
    Home,
    End,
    PageUp,
    PageDown
}
