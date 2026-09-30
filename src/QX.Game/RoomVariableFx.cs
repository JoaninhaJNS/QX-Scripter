using System.Globalization;
using Qx.Model.Messages.Incoming;

namespace Qx.Game;

/// <summary>Represents one Fx bar value in the room, which is the value of a wired variable on an avatar or furni.</summary>
/// <remarks>The value carries the configuration the room draws it with.</remarks>
/// <param name="Slot">The avatar or furni and the variable the value belongs to.</param>
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
    /// <summary>Gets the id of the configuration the value is drawn with.</summary>
    public int ConfigId => Slot.ConfigId;

    /// <summary>Gets the id of the wired variable the value belongs to.</summary>
    public string VariableId => Slot.VariableId;

    /// <summary>Gets whether the value belongs to an avatar rather than a furni.</summary>
    public bool IsUser => Slot.IsUserEntity;

    /// <summary>Gets the avatar's room index, or the furni's item id.</summary>
    public int EntityId => Slot.EntityId;

    /// <summary>Gets the icon of the configuration, such as <c>gold</c> or <c>ranch.tomato</c>, or <see langword="null"/> while the configuration has not arrived.</summary>
    public string? Icon => Config?.Icon;

    /// <summary>Gets the lower bound, which is the server's override, else the configuration's default, else 0.</summary>
    public long MinValue => MinOverride ?? Config?.DefaultMinValue ?? 0;

    /// <summary>Gets the upper bound, which is the server's override, else the configuration's default, else 0.</summary>
    public long MaxValue => MaxOverride ?? Config?.DefaultMaxValue ?? 0;

    /// <summary>Gets the level a leveling variable has reached, or <see langword="null"/> when the value has no level.</summary>
    public double? Level => ExtraNumber("current_level");

    /// <summary>Gets the highest level a leveling variable can reach, or <see langword="null"/> when the value has no level.</summary>
    public double? MaxLevel => ExtraNumber("max_level");

    /// <summary>Gets whether a leveling variable has reached its highest level.</summary>
    /// <remarks>As in the client, only an <c>is_maxed</c> value of <c>true</c>, ignoring case, counts.</remarks>
    public bool IsMaxed =>
        Extra.TryGetValue("is_maxed", out string? text) &&
        string.Equals(text.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether the value belongs to the variable with the specified id or is drawn with the specified icon.</summary>
    /// <param name="variable">The variable id or icon to match, compared case sensitively.</param>
    public bool Matches(string variable) =>
        string.Equals(VariableId, variable, StringComparison.Ordinal) ||
        string.Equals(Icon, variable, StringComparison.Ordinal);

    private double? ExtraNumber(string key) =>
        Extra.TryGetValue(key, out string? text) &&
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            ? number
            : null;
}
