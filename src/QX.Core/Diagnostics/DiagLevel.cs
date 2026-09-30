namespace Qx.Diagnostics;

/// <summary>Specifies the severity of a diagnostic message, from least to most severe.</summary>
public enum DiagLevel
{
    /// <summary>Fine-grained tracing output.</summary>
    Trace,
    /// <summary>Debugging output.</summary>
    Debug,
    /// <summary>Informational messages.</summary>
    Info,
    /// <summary>Warnings about recoverable problems.</summary>
    Warn,
    /// <summary>Errors.</summary>
    Error
}
