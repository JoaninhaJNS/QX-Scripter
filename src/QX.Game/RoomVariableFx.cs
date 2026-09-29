using System.Globalization;
using Qx.Model.Messages.Incoming;

namespace Qx.Game;

/// <summary>
/// One Fx bar value in the room: the value of one wired variable on one avatar or furni, together
/// with the configuration the room draws it with.
/// </summary>
/// <param name="Slot">Which avatar or furni and which variable the value belongs to.</param>
/// <param name="Value">The current value.</param>
/// <param name="MinOverride">The lower bound the server set for this value, if any.</param>
/// <param name="MaxOverride">The upper bound the server set for this value, if any.</param>
/// <param name="Extra">Additional renderer data the server sent with the value, such as <c>current_level</c>.</param>
/// <param name="IsInitialize">Whether the value was restated on entry rather than changed.</param>
/// <param name="Config">The configuration of the value, or <see langword="null"/> while it has not arrived.</param>
/// <param name="Revision">The room revision the value was applied at.</param>
/// <param name="Timestamp">When the value was received, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
public sealed record VariableFxValue(
    VariableFxSlot Slot,
    long Value,
    long? MinOverride,
    long? MaxOverride,
    IReadOnlyDictionary<string, string> Extra,
    bool IsInitialize,
    VariableFxConfigEntry? Config,
    long Revision,
    long Timestamp)
{
    public int ConfigId => Slot.ConfigId;

    public string VariableId => Slot.VariableId;

    /// <summary>Whether the value belongs to an avatar; otherwise it belongs to a furni.</summary>
    public bool IsUser => Slot.IsUserEntity;

    /// <summary>The avatar's room index, or the furni's item id.</summary>
    public int EntityId => Slot.EntityId;

    /// <summary>The icon of the configuration, such as <c>gold</c> or <c>ranch.tomato</c>.</summary>
    public string? Icon => Config?.Icon;

    /// <summary>The lower bound: the server's override, else the configuration's default.</summary>
    public long MinValue => MinOverride ?? Config?.DefaultMinValue ?? 0;

    /// <summary>The upper bound: the server's override, else the configuration's default.</summary>
    public long MaxValue => MaxOverride ?? Config?.DefaultMaxValue ?? 0;

    /// <summary>The level a levelling variable has reached, or <see langword="null"/> when the value has no level.</summary>
    public double? Level => ExtraNumber("current_level");

    /// <summary>The highest level a levelling variable can reach, or <see langword="null"/> when the value has no level.</summary>
    public double? MaxLevel => ExtraNumber("max_level");

    /// <summary>Whether a levelling variable has reached its highest level, read like the client: only <c>true</c> counts.</summary>
    public bool IsMaxed =>
        Extra.TryGetValue("is_maxed", out string? text) &&
        string.Equals(text.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the value belongs to the variable with this id or is drawn with this icon.</summary>
    public bool Matches(string variable) =>
        string.Equals(VariableId, variable, StringComparison.Ordinal) ||
        string.Equals(Icon, variable, StringComparison.Ordinal);

    private double? ExtraNumber(string key) =>
        Extra.TryGetValue(key, out string? text) &&
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            ? number
            : null;
}
