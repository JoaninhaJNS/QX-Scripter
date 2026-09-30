# Platform

Scripts run the same on Windows, macOS and Linux. `Os` and `Keyboard` cover what differs, so a
script needs no platform code.

## Operating system

| Member | Description |
| --- | --- |
| `Os.Kind` | `Windows`, `MacOS`, `Linux` or `Other`. |
| `Os.IsWindows`, `Os.IsMacOS`, `Os.IsLinux` | Shortcuts for `Kind`. |
| `Os.Name` | A readable name, such as `Windows 11 (build 26200)`. |
| `Os.Version`, `Os.Architecture` | The version and the processor architecture. |
| `Os.Display` | `Native`, `X11`, `XWayland`, `Wayland` or `None`. |

```csharp
Log($"{Os.Name} on {Os.Architecture}");
```

## Keyboard

`Keyboard` reads the physical keyboard system-wide on Windows, macOS and X11.

```csharp
if (!Keyboard.IsSupported)
{
    Log(Keyboard.Status);
    return;
}

Keyboard.OnDown(Key.F3, () => Talk("F3 pressed"));
await Wait();
```

| Member | Description |
| --- | --- |
| `IsSupported` | Whether keys can be read on this system. |
| `Status` | How keys are read, or why they cannot be. |
| `IsDown(key)` | Whether a key is held down now. |
| `OnDown(key, handler)`, `OnUp(key, handler)` | Runs a handler once per press or release. |

On macOS the app needs the Input Monitoring permission. A Wayland session does not allow reading the
keyboard; `Status` says so. Handlers are removed when the script stops.

Keys: `A` to `Z`, `D0` to `D9`, `F1` to `F12`, `Shift`, `Control`, `Alt`, `Space`, `Enter`,
`Escape`, `Tab`, `Backspace`, the arrows, `Insert`, `Delete`, `Home`, `End`, `PageUp` and
`PageDown`.
