using System.Diagnostics;

namespace Qx.Diagnostics;

/// <summary>Provides a process-wide diagnostic log that raises <see cref="Emitted"/> for each accepted message.</summary>
public static class Diag
{
    /// <summary>Gets whether this assembly was compiled with the <c>QX_DEBUG</c> symbol.</summary>
    public static bool IsBuiltWithDebug =>
#if QX_DEBUG
        true;
#else
        false;
#endif

    /// <summary>Gets or sets whether messages are emitted at all.</summary>
    /// <remarks>Defaults to <see cref="IsBuiltWithDebug"/>.</remarks>
    public static bool Enabled { get; set; } = IsBuiltWithDebug;

    /// <summary>Gets or sets the lowest level that is emitted.</summary>
    /// <remarks>Defaults to <see cref="DiagLevel.Trace"/>.</remarks>
    public static DiagLevel MinLevel { get; set; } = DiagLevel.Trace;

    /// <summary>Occurs when a message passes the <see cref="Enabled"/> and <see cref="MinLevel"/> filters.</summary>
    /// <remarks>The arguments are the level, the message and the optional category.</remarks>
    public static event Action<DiagLevel, string, string?>? Emitted;

    /// <summary>Emits a message when diagnostics are enabled and <paramref name="level"/> is at least <see cref="MinLevel"/>.</summary>
    /// <param name="level">The severity of the message.</param>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    public static void Log(DiagLevel level, string message, string? category = null)
    {
        if (!Enabled || level < MinLevel)
            return;
        Emitted?.Invoke(level, message, category);
    }

    /// <summary>Emits a message at <see cref="DiagLevel.Trace"/>.</summary>
    /// <remarks>Calls are compiled only when the calling code defines <c>QX_DEBUG</c>.</remarks>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    [Conditional("QX_DEBUG")]
    public static void Trace(string message, string? category = null) => Log(DiagLevel.Trace, message, category);

    /// <summary>Emits a message at <see cref="DiagLevel.Debug"/>.</summary>
    /// <remarks>Calls are compiled only when the calling code defines <c>QX_DEBUG</c>.</remarks>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    [Conditional("QX_DEBUG")]
    public static void Debug(string message, string? category = null) => Log(DiagLevel.Debug, message, category);

    /// <summary>Emits a message at <see cref="DiagLevel.Info"/>.</summary>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    public static void Info(string message, string? category = null) => Log(DiagLevel.Info, message, category);

    /// <summary>Emits a message at <see cref="DiagLevel.Warn"/>.</summary>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    public static void Warn(string message, string? category = null) => Log(DiagLevel.Warn, message, category);

    /// <summary>Emits a message at <see cref="DiagLevel.Error"/>.</summary>
    /// <param name="message">The message text.</param>
    /// <param name="category">The source category, or <see langword="null"/> for none.</param>
    public static void Error(string message, string? category = null) => Log(DiagLevel.Error, message, category);
}
