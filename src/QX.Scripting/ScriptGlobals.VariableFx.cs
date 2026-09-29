using Qx.Game;
using Qx.Model;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Every Fx bar value in the room: the wired variables the room shows above avatars and furni,
    /// such as coins, orders or growth. Kept as room state, so a script started later still sees
    /// what the room sent on entry.
    /// </summary>
    public IReadOnlyList<VariableFxValue> VariableFx => Room.VariableFxValues;

    /// <summary>
    /// The Fx bar configurations of the room, which tell how each value is drawn and carry its
    /// <see cref="VariableFxConfigEntry.Icon"/>.
    /// </summary>
    public IReadOnlyList<VariableFxConfigEntry> VariableFxConfigs => Room.VariableFxConfigs;

    /// <summary>The Fx bar values shown above an avatar.</summary>
    public IReadOnlyList<VariableFxValue> VariableFxOf(Avatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        return Room.VariableFxOf(true, avatar.Index);
    }

    /// <summary>The Fx bar values shown on a furni.</summary>
    public IReadOnlyList<VariableFxValue> VariableFxOf(FloorItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Room.VariableFxOf(false, item.Id);
    }

    /// <summary>
    /// The Fx bar value of one variable above an avatar, or <see langword="null"/> when it shows
    /// none.
    /// </summary>
    /// <param name="avatar">The avatar.</param>
    /// <param name="variable">The variable id, or else the icon it is drawn with, such as <c>gold</c>.</param>
    public VariableFxValue? VariableFxOf(Avatar avatar, string variable) =>
        find_variable_fx(VariableFxOf(avatar), variable);

    /// <summary>
    /// The Fx bar value of one variable on a furni, or <see langword="null"/> when it shows none.
    /// </summary>
    /// <param name="item">The furni.</param>
    /// <param name="variable">The variable id, or else the icon it is drawn with, such as <c>upgrading</c>.</param>
    public VariableFxValue? VariableFxOf(FloorItem item, string variable) =>
        find_variable_fx(VariableFxOf(item), variable);

    /// <summary>
    /// Reads one wired variable of an avatar from wherever the room exposes it: the Fx bar when
    /// the room shows the variable there, otherwise an inspection when you may read wired in this
    /// room. Without wired read rights, the Fx bar is the only variable data the hotel sends.
    /// </summary>
    /// <param name="avatar">The avatar.</param>
    /// <param name="variable">
    /// The variable id or Fx icon; with wired read rights also the variable's display name.
    /// </param>
    /// <param name="timeoutMs">How long to wait for an inspection, in milliseconds.</param>
    /// <returns>The value, or <see langword="null"/> when no source has it.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">An inspection did not answer in time.</exception>
    public async Task<long?> GetVariableValue(Avatar avatar, string variable, int timeoutMs = 10000)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        if (VariableFxOf(avatar, variable) is { } shown)
            return shown.Value;
        if (!CanReadWired)
            return null;
        return (await GetUserValues(avatar.Index, timeoutMs))[variable];
    }

    /// <summary>
    /// Reads one wired variable of a furni from wherever the room exposes it: the Fx bar when the
    /// room shows the variable there, otherwise an inspection when you may read wired in this
    /// room. Without wired read rights, the Fx bar is the only variable data the hotel sends.
    /// </summary>
    /// <param name="item">The furni.</param>
    /// <param name="variable">
    /// The variable id or Fx icon; with wired read rights also the variable's display name.
    /// </param>
    /// <param name="timeoutMs">How long to wait for an inspection, in milliseconds.</param>
    /// <returns>The value, or <see langword="null"/> when no source has it.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">An inspection did not answer in time.</exception>
    public async Task<long?> GetVariableValue(FloorItem item, string variable, int timeoutMs = 10000)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (VariableFxOf(item, variable) is { } shown)
            return shown.Value;
        if (!CanReadWired)
            return null;
        return (await GetFurniValues(item.Id, timeoutMs))[variable];
    }

    /// <summary>
    /// Subscribes to Fx bar values being set or changed on any avatar or furni.
    /// </summary>
    /// <param name="handler">
    /// Receives the value as it is now and as it was before, which is <see langword="null"/> for a
    /// value that is new.
    /// </param>
    /// <returns>A handle that unsubscribes when disposed; also disposed when the script stops.</returns>
    public IDisposable OnVariableFxChanged(Action<VariableFxValue, VariableFxValue?> handler) =>
        Subscribe(
            handler,
            listener => Room.VariableFxChanged += listener,
            listener => Room.VariableFxChanged -= listener);

    /// <summary>Subscribes to Fx bar values being removed from an avatar or furni.</summary>
    /// <param name="handler">Receives the value as it was.</param>
    /// <returns>A handle that unsubscribes when disposed; also disposed when the script stops.</returns>
    public IDisposable OnVariableFxRemoved(Action<VariableFxValue> handler) =>
        Subscribe(
            handler,
            listener => Room.VariableFxRemoved += listener,
            listener => Room.VariableFxRemoved -= listener);

    private static VariableFxValue? find_variable_fx(IReadOnlyList<VariableFxValue> values, string variable)
    {
        ArgumentException.ThrowIfNullOrEmpty(variable);
        return values.FirstOrDefault(value => value.VariableId == variable) ??
            values.FirstOrDefault(value => value.Icon == variable);
    }
}
