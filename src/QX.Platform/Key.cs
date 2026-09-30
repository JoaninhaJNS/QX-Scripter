namespace Qx.Platform;

/// <summary>Specifies a key on the keyboard, independent of the operating system that reads it.</summary>
/// <remarks>The digit keys <see cref="D0"/> to <see cref="D9"/> are the keys on the main row, not the numeric keypad.</remarks>
public enum Key
{
    /// <summary>The A key.</summary>
    A,
    /// <summary>The B key.</summary>
    B,
    /// <summary>The C key.</summary>
    C,
    /// <summary>The D key.</summary>
    D,
    /// <summary>The E key.</summary>
    E,
    /// <summary>The F key.</summary>
    F,
    /// <summary>The G key.</summary>
    G,
    /// <summary>The H key.</summary>
    H,
    /// <summary>The I key.</summary>
    I,
    /// <summary>The J key.</summary>
    J,
    /// <summary>The K key.</summary>
    K,
    /// <summary>The L key.</summary>
    L,
    /// <summary>The M key.</summary>
    M,
    /// <summary>The N key.</summary>
    N,
    /// <summary>The O key.</summary>
    O,
    /// <summary>The P key.</summary>
    P,
    /// <summary>The Q key.</summary>
    Q,
    /// <summary>The R key.</summary>
    R,
    /// <summary>The S key.</summary>
    S,
    /// <summary>The T key.</summary>
    T,
    /// <summary>The U key.</summary>
    U,
    /// <summary>The V key.</summary>
    V,
    /// <summary>The W key.</summary>
    W,
    /// <summary>The X key.</summary>
    X,
    /// <summary>The Y key.</summary>
    Y,
    /// <summary>The Z key.</summary>
    Z,
    /// <summary>The 0 key on the main row.</summary>
    D0,
    /// <summary>The 1 key on the main row.</summary>
    D1,
    /// <summary>The 2 key on the main row.</summary>
    D2,
    /// <summary>The 3 key on the main row.</summary>
    D3,
    /// <summary>The 4 key on the main row.</summary>
    D4,
    /// <summary>The 5 key on the main row.</summary>
    D5,
    /// <summary>The 6 key on the main row.</summary>
    D6,
    /// <summary>The 7 key on the main row.</summary>
    D7,
    /// <summary>The 8 key on the main row.</summary>
    D8,
    /// <summary>The 9 key on the main row.</summary>
    D9,
    /// <summary>The F1 key.</summary>
    F1,
    /// <summary>The F2 key.</summary>
    F2,
    /// <summary>The F3 key.</summary>
    F3,
    /// <summary>The F4 key.</summary>
    F4,
    /// <summary>The F5 key.</summary>
    F5,
    /// <summary>The F6 key.</summary>
    F6,
    /// <summary>The F7 key.</summary>
    F7,
    /// <summary>The F8 key.</summary>
    F8,
    /// <summary>The F9 key.</summary>
    F9,
    /// <summary>The F10 key.</summary>
    F10,
    /// <summary>The F11 key.</summary>
    F11,
    /// <summary>The F12 key.</summary>
    F12,

    /// <summary>Either shift key.</summary>
    Shift,

    /// <summary>Either control key.</summary>
    Control,

    /// <summary>Either alt key; option on macOS.</summary>
    Alt,

    /// <summary>The space bar.</summary>
    Space,
    /// <summary>Either enter key, including the one on the numeric keypad.</summary>
    Enter,
    /// <summary>The escape key.</summary>
    Escape,
    /// <summary>The tab key.</summary>
    Tab,
    /// <summary>The backspace key, labeled delete on macOS keyboards.</summary>
    Backspace,
    /// <summary>The left arrow key.</summary>
    Left,
    /// <summary>The up arrow key.</summary>
    Up,
    /// <summary>The right arrow key.</summary>
    Right,
    /// <summary>The down arrow key.</summary>
    Down,
    /// <summary>The insert key.</summary>
    Insert,
    /// <summary>The forward delete key.</summary>
    Delete,
    /// <summary>The home key.</summary>
    Home,
    /// <summary>The end key.</summary>
    End,
    /// <summary>The page up key.</summary>
    PageUp,
    /// <summary>The page down key.</summary>
    PageDown
}
