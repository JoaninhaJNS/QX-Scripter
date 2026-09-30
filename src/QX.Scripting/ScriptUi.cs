using System.Collections.Concurrent;
using System.Globalization;

namespace Qx.Scripting;

/// <summary>
/// Represents the panel a script declared with <c>//@ui:</c> directives.
/// </summary>
/// <remarks>
/// <para>
/// It holds the values a user entered and the ways of writing back to the panel while the script
/// runs.
/// </para>
/// <para>
/// Outside panel mode every getter returns its fallback, <see cref="Clicked"/> is always
/// <see langword="false"/>, and the writers do nothing. A script can therefore use it
/// unconditionally and still run from the editor.
/// </para>
/// </remarks>
public sealed class ScriptUi
{
    readonly ConcurrentDictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Func<Task>>> _handlers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _handler_sync = new();
    string? _clicked;

    /// <summary>
    /// Registers a handler that runs when a panel button is pressed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A script that registers a handler keeps running: pressing the button calls the handler
    /// rather than starting the script again. That is the difference between a panel and a form,
    /// and it is why a panel does not need a loop watching <see cref="Clicked"/> to stay responsive.
    /// </para>
    /// <para>
    /// Handlers run alongside one another, so a button that works for a minute does not stop the
    /// others from answering. A script that does not want that can say so itself, by disabling the
    /// button with <see cref="Enable"/> while it works.
    /// </para>
    /// <para>
    /// Registering the same button twice adds a second handler rather than replacing the first;
    /// both run, and one of them throwing does not stop the other. A handler cannot be removed.
    /// </para>
    /// </remarks>
    /// <param name="button">The button's name, as the directive declared it, matched case-insensitively.</param>
    /// <param name="handler">The handler to call when the button is pressed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="button"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public void OnClick(string button, Func<Task> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(button);
        ArgumentNullException.ThrowIfNull(handler);
        lock (_handler_sync)
        {
            if (!_handlers.TryGetValue(button, out List<Func<Task>>? list))
                _handlers[button] = list = [];
            list.Add(handler);
        }
    }

    /// <inheritdoc cref="OnClick(string, Func{Task})"/>
    public void OnClick(string button, Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        OnClick(button, () =>
        {
            handler();
            return Task.CompletedTask;
        });
    }

    /// <summary>Gets whether any button has a handler, which is what keeps a panel script alive.</summary>
    public bool HasClickHandlers
    {
        get
        {
            lock (_handler_sync)
                return _handlers.Count > 0;
        }
    }

    /// <summary>Gets a copy of the names of the buttons that have a handler.</summary>
    public IReadOnlyCollection<string> HandledButtons
    {
        get
        {
            lock (_handler_sync)
                return _handlers.Keys.ToArray();
        }
    }

    /// <summary>
    /// Runs the handlers registered for a button.
    /// </summary>
    /// <remarks>
    /// The host calls it when the button is pressed; scripts do not. It also records the button as
    /// the one <see cref="ClickedButton"/> reports. Every handler is started, and one that throws
    /// before returning a task gives a faulted task instead of stopping the others.
    /// </remarks>
    /// <param name="button">The button's name.</param>
    /// <returns>
    /// A task that completes when every handler has finished, or <see langword="null"/> when the
    /// button has none, which lets the host tell "nothing happened" from "something started".
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="button"/> is <see langword="null"/>.</exception>
    public Task? Invoke(string button)
    {
        ArgumentNullException.ThrowIfNull(button);

        Func<Task>[] handlers;
        lock (_handler_sync)
        {
            if (!_handlers.TryGetValue(button, out List<Func<Task>>? list) || list.Count == 0)
                return null;
            handlers = list.ToArray();
        }

        Volatile.Write(ref _clicked, button);

        // Each handler is started inside its own try and a synchronous throw is turned into a
        // faulted task. Handing the delegates straight to Task.WhenAll ran them lazily inside this
        // frame, so a handler that threw before returning a task threw out of Invoke: the host had
        // no task to attach its error reporting to, the exception surfaced in a click callback with
        // nothing to catch it, and the handlers after it never ran at all. An Action handler always
        // takes that path, because its wrapper runs the body before returning.
        var running = new Task[handlers.Length];
        for (int i = 0; i < handlers.Length; i++)
        {
            try
            {
                running[i] = handlers[i]() ?? Task.CompletedTask;
            }
            catch (Exception error)
            {
                running[i] = Task.FromException(error);
            }
        }
        return Task.WhenAll(running);
    }

    /// <summary>Occurs when the script writes a line to an output box.</summary>
    /// <remarks>The arguments are the box name and the text.</remarks>
    public event Action<string, string>? Logged;

    /// <summary>Occurs when the script offers a file for download.</summary>
    /// <remarks>The arguments are the suggested file name and the contents.</remarks>
    public event Action<string, string>? Downloaded;

    /// <summary>Occurs when the script empties an output box or a table.</summary>
    /// <remarks>The argument is the name of the box or table.</remarks>
    public event Action<string>? Cleared;

    /// <summary>Occurs when the script changes a control's value.</summary>
    /// <remarks>The arguments are the control name and the new value.</remarks>
    public event Action<string, string>? Changed;

    /// <summary>Occurs when the script moves a progress bar.</summary>
    /// <remarks>The arguments are the bar name and the fraction done, between 0 and 1.</remarks>
    public event Action<string, double>? ProgressChanged;

    /// <summary>Occurs when the script replaces a status line.</summary>
    /// <remarks>The arguments are the line name and the new text.</remarks>
    public event Action<string, string>? StatusChanged;

    /// <summary>Occurs when the script enables or disables a control.</summary>
    /// <remarks>The arguments are the control name and whether it is enabled.</remarks>
    public event Action<string, bool>? EnabledChanged;

    /// <summary>Occurs when the script shows or hides a control.</summary>
    /// <remarks>The arguments are the control name and whether it is shown.</remarks>
    public event Action<string, bool>? VisibilityChanged;

    /// <summary>
    /// Changes a control's value, both for later reads and on screen.
    /// </summary>
    /// <param name="name">The control's name.</param>
    /// <param name="value">The new value, or <see langword="null"/> to store an empty string.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Set(string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        string stored = value ?? "";
        _values[name] = stored;
        Changed?.Invoke(name, stored);
    }

    /// <summary>Records which button started this run.</summary>
    /// <remarks>Called by the host, not by scripts.</remarks>
    /// <param name="button">The button's name, or <see langword="null"/> when the run was not started by one.</param>
    public void SetClicked(string? button) => Volatile.Write(ref _clicked, button);

    /// <summary>Gets whether a named button started this run.</summary>
    /// <remarks>
    /// The name is matched case-insensitively. Once handlers are registered, the last button whose
    /// handlers ran counts as the one that was clicked.
    /// </remarks>
    /// <param name="button">The button's name.</param>
    public bool Clicked(string button) =>
        string.Equals(Volatile.Read(ref _clicked), button, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the name of the button that started this run, or <see langword="null"/>.</summary>
    /// <remarks>Once handlers are registered, it is the last button whose handlers ran.</remarks>
    public string? ClickedButton => Volatile.Read(ref _clicked);

    /// <summary>Gets a text value, or the fallback when it is missing or empty.</summary>
    /// <remarks>
    /// For a selectable table it is the selected row, its cells joined with tabs.
    /// </remarks>
    /// <param name="name">The control's name.</param>
    /// <param name="fallback">The value to return when nothing was entered.</param>
    public string String(string name, string fallback = "") =>
        _values.TryGetValue(name, out string? v) && v.Length > 0 ? v : fallback;

    /// <inheritdoc cref="String"/>
    public string Text(string name, string fallback = "") => String(name, fallback);

    /// <inheritdoc cref="String"/>
    public string Select(string name, string fallback = "") => String(name, fallback);

    /// <summary>Gets a whole number, or the fallback when it is missing or unparsable.</summary>
    /// <param name="name">The control's name.</param>
    /// <param name="fallback">The value to return when nothing usable was entered.</param>
    public int Int(string name, int fallback = 0) =>
        _values.TryGetValue(name, out string? v) && int.TryParse(v, out int i) ? i : fallback;

    /// <summary>Gets a number, or the fallback when it is missing or unparsable.</summary>
    /// <remarks>The value is parsed with the invariant culture, so the decimal separator is a dot.</remarks>
    /// <param name="name">The control's name.</param>
    /// <param name="fallback">The value to return when nothing usable was entered.</param>
    public double Number(string name, double fallback = 0) =>
        _values.TryGetValue(name, out string? v) &&
        double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)
            ? d
            : fallback;

    /// <summary>Gets a checkbox value, or the fallback when it is missing.</summary>
    /// <remarks>
    /// Only <c>true</c>, <c>True</c> and <c>1</c> count as checked. Any other stored value gives
    /// <see langword="false"/>, not the fallback.
    /// </remarks>
    /// <param name="name">The control's name.</param>
    /// <param name="fallback">The value to return when the control was never set.</param>
    public bool Bool(string name, bool fallback = false) =>
        _values.TryGetValue(name, out string? v) ? v is "true" or "True" or "1" : fallback;

    /// <summary>Gets a chosen file path, or <see langword="null"/> when none was chosen.</summary>
    /// <param name="name">The control's name.</param>
    public string? File(string name) =>
        _values.TryGetValue(name, out string? v) && v.Length > 0 ? v : null;

    /// <summary>Gets the contents of a chosen file, or an empty string when there is none.</summary>
    /// <remarks>
    /// The file is read in full on every call. A path that no longer exists also gives an empty
    /// string.
    /// </remarks>
    /// <param name="name">The control's name.</param>
    public string FileText(string name) =>
        File(name) is { } path && System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "";

    /// <summary>
    /// Writes a line to an output box.
    /// </summary>
    /// <param name="box">
    /// The box's name. An empty name goes to the first box the panel declares, which is why a
    /// panel with several boxes should always name one.
    /// </param>
    /// <param name="text">The line. Its <c>ToString</c> is used, and <see langword="null"/> writes an empty line.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="box"/> is <see langword="null"/>.</exception>
    public void Log(string box, object? text)
    {
        ArgumentNullException.ThrowIfNull(box);
        Logged?.Invoke(box, text?.ToString() ?? "");
    }

    /// <summary>Empties an output box or a table.</summary>
    /// <remarks>
    /// When an output box and a table share the name, both are emptied.
    /// </remarks>
    /// <param name="box">The name of the box or table.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="box"/> is <see langword="null"/>.</exception>
    public void Clear(string box)
    {
        ArgumentNullException.ThrowIfNull(box);
        Cleared?.Invoke(box);
    }

    /// <summary>
    /// Moves a progress bar.
    /// </summary>
    /// <param name="name">The bar's name.</param>
    /// <param name="value">
    /// How far along, from 0 to 1. Values outside that range are clamped and NaN counts as 0, so a
    /// caller dividing by a total that turned out to be zero does not produce a bar drawn off its
    /// own end.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Progress(string name, double value)
    {
        ArgumentNullException.ThrowIfNull(name);
        double clamped = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
        ProgressChanged?.Invoke(name, clamped);
    }

    /// <summary>Moves a progress bar by a count rather than a fraction.</summary>
    /// <param name="name">The bar's name.</param>
    /// <param name="done">The number of items finished.</param>
    /// <param name="total">The number of items in total. Zero or less leaves the bar at nothing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Progress(string name, int done, int total) =>
        Progress(name, total <= 0 ? 0 : (double)done / total);

    /// <summary>Replaces a status line.</summary>
    /// <param name="name">The line's name.</param>
    /// <param name="text">The new text. Its <c>ToString</c> is used, and <see langword="null"/> clears the line.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Status(string name, object? text)
    {
        ArgumentNullException.ThrowIfNull(name);
        StatusChanged?.Invoke(name, text?.ToString() ?? "");
    }

    /// <summary>Enables or disables a control.</summary>
    /// <param name="name">The control's name.</param>
    /// <param name="enabled"><see langword="true"/> to let the control be used; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Enable(string name, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnabledChanged?.Invoke(name, enabled);
    }

    /// <summary>Shows or hides a control.</summary>
    /// <param name="name">The control's name.</param>
    /// <param name="visible">
    /// <see langword="true"/> to show the control; otherwise, <see langword="false"/>. A hidden
    /// control takes no space.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public void Show(string name, bool visible = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        VisibilityChanged?.Invoke(name, visible);
    }

    /// <summary>Offers a file for the user to save.</summary>
    /// <param name="fileName">The suggested name.</param>
    /// <param name="content">The contents.</param>
    public void Download(string fileName, string content) => Downloaded?.Invoke(fileName, content);

    /// <summary>Occurs when the script appends a row to a table.</summary>
    /// <remarks>The arguments are the table name and the cells, left to right.</remarks>
    public event Action<string, IReadOnlyList<string>>? RowAdded;

    /// <summary>Occurs when the script replaces every row of a table.</summary>
    /// <remarks>The arguments are the table name and the rows, top to bottom.</remarks>
    public event Action<string, IReadOnlyList<IReadOnlyList<string>>>? RowsSet;

    /// <summary>Occurs when the script wants a short message shown.</summary>
    /// <remarks>The arguments are the text and whether it reports a problem.</remarks>
    public event Action<string, bool>? Toasted;

    /// <summary>Occurs when the script marks a button as working, or done.</summary>
    /// <remarks>The arguments are the button name and whether it is working.</remarks>
    public event Action<string, bool>? BusyChanged;

    /// <summary>Occurs when the script asks the user to confirm something.</summary>
    /// <remarks>
    /// The arguments are the title and the message, and the handler returns the answer. Without a
    /// handler <see cref="Confirm"/> answers <see langword="false"/> at once.
    /// </remarks>
    public event Func<string, string, Task<bool>>? ConfirmRequested;

    /// <summary>Occurs when the script asks the user for a value.</summary>
    /// <remarks>
    /// The arguments are the title and the initial text, and the handler returns the answer or
    /// <see langword="null"/>. Without a handler <see cref="Prompt"/> answers
    /// <see langword="null"/> at once.
    /// </remarks>
    public event Func<string, string, Task<string?>>? PromptRequested;

    /// <summary>
    /// Appends a row to a table.
    /// </summary>
    /// <remarks>
    /// Cells are converted with <c>ToString</c>, and a null cell becomes an empty one rather than
    /// the word "null". Extra cells beyond the declared columns are kept but not shown.
    /// </remarks>
    /// <param name="table">The table's name.</param>
    /// <param name="cells">The row, left to right.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="table"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cells"/> is <see langword="null"/>.</exception>
    public void AddRow(string table, params object?[] cells)
    {
        ArgumentException.ThrowIfNullOrEmpty(table);
        ArgumentNullException.ThrowIfNull(cells);
        RowAdded?.Invoke(table, Cells(cells));
    }

    /// <summary>
    /// Replaces every row of a table in one step.
    /// </summary>
    /// <remarks>
    /// Rows that are still there are updated in place, so the table keeps its scroll position and
    /// its selection. That makes this the way to refresh a table on a timer; clearing it and adding
    /// the rows again throws the reader back to the top on every refresh.
    /// </remarks>
    /// <param name="table">The table's name.</param>
    /// <param name="rows">The rows, top to bottom, each one left to right. A <see langword="null"/> row becomes an empty one.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="table"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is <see langword="null"/>.</exception>
    public void SetRows(string table, IEnumerable<object?[]> rows)
    {
        ArgumentException.ThrowIfNullOrEmpty(table);
        ArgumentNullException.ThrowIfNull(rows);
        RowsSet?.Invoke(table, [.. rows.Select(row => Cells(row ?? []))]);
    }

    static string[] Cells(object?[] cells) => [.. cells.Select(cell => cell?.ToString() ?? "")];

    /// <summary>
    /// Shows a short message that fades on its own.
    /// </summary>
    /// <remarks>
    /// For something worth noticing but not worth a line in an output box. A message marked as a
    /// problem is tinted, so a script can say "that did not work" without a box for it.
    /// </remarks>
    /// <param name="text">The message to show.</param>
    /// <param name="problem"><see langword="true"/> when the message reports something going wrong; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    public void Toast(string text, bool problem = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        Toasted?.Invoke(text, problem);
    }

    /// <summary>
    /// Marks a button as working, so it shows that something is happening.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Enable"/>: a button can be busy and still clickable, and a script
    /// that wants both says both. The panel clears it by itself when the handler returns, so this
    /// is only needed for work a script starts elsewhere.
    /// </remarks>
    /// <param name="button">The button's name.</param>
    /// <param name="busy"><see langword="true"/> to mark the button as working; <see langword="false"/> to clear the mark.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="button"/> is <see langword="null"/> or empty.</exception>
    public void Busy(string button, bool busy = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(button);
        BusyChanged?.Invoke(button, busy);
    }

    /// <summary>
    /// Asks the user to confirm something and waits for the answer.
    /// </summary>
    /// <remarks>
    /// Outside panel mode, and anywhere else with nobody to ask, this answers no at once rather
    /// than waiting: a script running headless must not hang on a question that will never be seen,
    /// and refusing is the safe half of a yes-or-no.
    /// </remarks>
    /// <param name="title">The heading.</param>
    /// <param name="message">The question being confirmed.</param>
    /// <returns>
    /// <see langword="true"/> when the user confirmed; <see langword="false"/> when they declined
    /// or nobody can be asked.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="title"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    public Task<bool> Confirm(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        return ConfirmRequested?.Invoke(title, message) ?? Task.FromResult(false);
    }

    /// <summary>
    /// Asks the user for a value and waits for it.
    /// </summary>
    /// <remarks>
    /// Answers <see langword="null"/> at once where there is nobody to ask, the same way and for
    /// the same reason as <see cref="Confirm"/>. Null also means the user dismissed the question,
    /// which is not the same as an empty answer.
    /// </remarks>
    /// <param name="title">The heading.</param>
    /// <param name="initial">The text the input box starts with.</param>
    /// <returns>
    /// The value entered, or <see langword="null"/> when the question was dismissed or nobody can
    /// be asked.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="title"/> or <paramref name="initial"/> is <see langword="null"/>.</exception>
    public Task<string?> Prompt(string title, string initial = "")
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(initial);
        return PromptRequested?.Invoke(title, initial) ?? Task.FromResult<string?>(null);
    }
}
