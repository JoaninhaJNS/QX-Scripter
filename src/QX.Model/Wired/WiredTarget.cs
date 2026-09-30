namespace Qx.Model.Wired;

/// <summary>Specifies what a wired variable belongs to.</summary>
/// <remarks>The values match the <see cref="WiredVariableTarget"/> constants.</remarks>
public enum WiredTarget
{
    /// <summary>A furni item, value 0.</summary>
    Furni = WiredVariableTarget.Furni,
    /// <summary>A room user, value 1.</summary>
    User = WiredVariableTarget.User,
    /// <summary>The merged target, value 2.</summary>
    Merged = WiredVariableTarget.Merged,
    /// <summary>The room as a whole, value -10.</summary>
    Global = WiredVariableTarget.Global,
    /// <summary>The context target, value -20.</summary>
    Context = WiredVariableTarget.Context,
}
