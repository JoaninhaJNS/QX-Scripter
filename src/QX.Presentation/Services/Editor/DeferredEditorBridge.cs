using Qx.Mcp;

namespace Qx.Presentation.Services.Editor;

public sealed class DeferredEditorBridge : IEditorBridge
{
    public const string Unavailable = "editor UI not available";

    IEditorBridge? _target;

    public void Attach(IEditorBridge target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(target, this))
            throw new ArgumentException("The bridge cannot forward to itself.", nameof(target));
        Volatile.Write(ref _target, target);
    }

    public void Detach() => Volatile.Write(ref _target, null);

    public Task<string> ListTabsAsync(CancellationToken cancellation_token) =>
        Target?.ListTabsAsync(cancellation_token) ?? MissingAsync();

    public Task<string> OpenTabAsync(string name, CancellationToken cancellation_token) =>
        Target?.OpenTabAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> CreateTabAsync(string name, string code, CancellationToken cancellation_token) =>
        Target?.CreateTabAsync(name, code, cancellation_token) ?? MissingAsync();

    public Task<string> EditActiveTabAsync(string code, CancellationToken cancellation_token) =>
        Target?.EditActiveTabAsync(code, cancellation_token) ?? MissingAsync();

    public Task<string> SelectTabAsync(string name, CancellationToken cancellation_token) =>
        Target?.SelectTabAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> CloseTabAsync(string name, bool discard, CancellationToken cancellation_token) =>
        Target?.CloseTabAsync(name, discard, cancellation_token) ?? MissingAsync();

    public Task<string> RunActiveTabAsync(string name, CancellationToken cancellation_token) =>
        Target?.RunActiveTabAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> StopActiveTabAsync(string name, CancellationToken cancellation_token) =>
        Target?.StopActiveTabAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> GetTabOutputAsync(string name, CancellationToken cancellation_token) =>
        Target?.GetTabOutputAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> GetTabStatusAsync(string name, CancellationToken cancellation_token) =>
        Target?.GetTabStatusAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string> GetTabErrorsAsync(string name, CancellationToken cancellation_token) =>
        Target?.GetTabErrorsAsync(name, cancellation_token) ?? MissingAsync();

    public Task<string?> ReadOpenScriptAsync(string name, CancellationToken cancellation_token) =>
        Target?.ReadOpenScriptAsync(name, cancellation_token) ?? Task.FromResult<string?>(null);

    public Task<string?> EditOpenScriptAsync(string name, Func<string, string> edit, CancellationToken cancellation_token) =>
        Target?.EditOpenScriptAsync(name, edit, cancellation_token) ?? Task.FromResult<string?>(null);

    public Task<string?> RenameScriptAsync(string name, string newName, CancellationToken cancellation_token) =>
        Target?.RenameScriptAsync(name, newName, cancellation_token) ?? Task.FromResult<string?>(null);

    public Task<string?> DeleteScriptAsync(string name, CancellationToken cancellation_token) =>
        Target?.DeleteScriptAsync(name, cancellation_token) ?? Task.FromResult<string?>(null);

    IEditorBridge? Target => Volatile.Read(ref _target);

    static Task<string> MissingAsync() => Task.FromResult(Unavailable);
}
