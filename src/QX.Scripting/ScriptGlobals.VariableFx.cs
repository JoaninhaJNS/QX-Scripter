using Qx.Game;
using Qx.Model;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Gets every Fx bar value in the room, which are the wired variables the room shows above
    /// avatars and furni, such as coins, orders or growth.
    /// </summary>
    /// <remarks>
    /// The values are kept as room state, so a script started later still sees what the room sent
    /// on entry. Every read returns a new list, with each value bound to its current configuration.
    /// </remarks>
    public IReadOnlyList<VariableFxValue> VariableFx => Room.VariableFxValues;

    /// <summary>
    /// Gets the Fx bar configurations of the room, which tell how each value is drawn and carry its
    /// <see cref="VariableFxConfigEntry.Icon"/>.
    /// </summary>
    public IReadOnlyList<VariableFxConfigEntry> VariableFxConfigs => Room.VariableFxConfigs;

    /// <summary>Gets the Fx bar values shown above an avatar.</summary>
    /// <param name="avatar">The avatar, matched by its room index.</param>
    /// <returns>The avatar's Fx bar values, or an empty list when it shows none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="avatar"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<VariableFxValue> VariableFxOf(Avatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        return Room.VariableFxOf(true, avatar.Index);
    }

    /// <summary>Gets the Fx bar values shown on a furni.</summary>
    /// <param name="item">The furni, matched by its item id.</param>
    /// <returns>The furni's Fx bar values, or an empty list when it shows none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<VariableFxValue> VariableFxOf(FloorItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Room.VariableFxOf(false, item.Id);
    }

    /// <summary>
    /// Gets the Fx bar value of one variable above an avatar.
    /// </summary>
    /// <remarks>
    /// The variable id is matched first, then the icon, both case sensitively.
    /// </remarks>
    /// <param name="avatar">The avatar, matched by its room index.</param>
    /// <param name="variable">The variable id, or else the icon it is drawn with, such as <c>gold</c>.</param>
    /// <returns>The value, or <see langword="null"/> when the avatar shows none for that variable.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variable"/> is <see langword="null"/> or empty.</exception>
    public VariableFxValue? VariableFxOf(Avatar avatar, string variable) =>
        find_variable_fx(VariableFxOf(avatar), variable);

    /// <summary>
    /// Gets the Fx bar value of one variable on a furni.
    /// </summary>
    /// <remarks>
    /// The variable id is matched first, then the icon, both case sensitively.
    /// </remarks>
    /// <param name="item">The furni, matched by its item id.</param>
    /// <param name="variable">The variable id, or else the icon it is drawn with, such as <c>upgrading</c>.</param>
    /// <returns>The value, or <see langword="null"/> when the furni shows none for that variable.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variable"/> is <see langword="null"/> or empty.</exception>
    public VariableFxValue? VariableFxOf(FloorItem item, string variable) =>
        find_variable_fx(VariableFxOf(item), variable);

    /// <summary>
    /// Reads one wired variable of an avatar from wherever the room exposes it.
    /// </summary>
    /// <remarks>
    /// The Fx bar is used when the room shows the variable there. Otherwise, when the local user may
    /// read wired in this room, the avatar's values are inspected with
    /// <see cref="GetUserValues(int, int)"/>. Without wired read rights, the Fx bar is the only
    /// variable data the hotel sends.
    /// </remarks>
    /// <param name="avatar">The avatar.</param>
    /// <param name="variable">
    /// The variable id or Fx icon; with wired read rights also the variable's display name.
    /// </param>
    /// <param name="timeoutMs">The timeout in milliseconds for each reply an inspection waits for.</param>
    /// <returns>The value, or <see langword="null"/> when no source has it.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when an inspection did not answer in time.</exception>
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
    /// Reads one wired variable of a furni from wherever the room exposes it.
    /// </summary>
    /// <remarks>
    /// The Fx bar is used when the room shows the variable there. Otherwise, when the local user may
    /// read wired in this room, the furni's values are inspected with
    /// <see cref="GetFurniValues(Id, int)"/>. Without wired read rights, the Fx bar is the only
    /// variable data the hotel sends.
    /// </remarks>
    /// <param name="item">The furni.</param>
    /// <param name="variable">
    /// The variable id or Fx icon; with wired read rights also the variable's display name.
    /// </param>
    /// <param name="timeoutMs">The timeout in milliseconds for each reply an inspection waits for.</param>
    /// <returns>The value, or <see langword="null"/> when no source has it.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when an inspection did not answer in time.</exception>
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
    /// Registers a handler that runs when an Fx bar value is set or changed on any avatar or furni.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the value as it is now and as it was before, which is
    /// <see langword="null"/> for a value that is new.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    public IDisposable OnVariableFxChanged(Action<VariableFxValue, VariableFxValue?> handler) =>
        Subscribe(
            handler,
            listener => Room.VariableFxChanged += listener,
            listener => Room.VariableFxChanged -= listener);

    /// <summary>Registers a handler that runs when an Fx bar value is removed from an avatar or furni.</summary>
    /// <param name="handler">The handler to call with the value as it was.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
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
