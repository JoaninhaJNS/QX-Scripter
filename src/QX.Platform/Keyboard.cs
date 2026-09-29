namespace Qx.Platform;

/// <summary>
/// Reads the physical keyboard system-wide, whichever window has focus. One instance is shared by
/// everything in the process: key watchers are polled together on one background thread, which
/// only runs while something is being watched.
/// </summary>
public sealed class Keyboard : IDisposable
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    readonly object _sync = new();
    readonly List<Watcher> _watchers = [];
    readonly ManualResetEventSlim _stopped = new(false);
    IKeyReader _reader;
    Thread? _poller;
    bool _disposed;

    Keyboard(IKeyReader reader) => _reader = reader;

    /// <summary>Opens the keyboard of the system QX runs on.</summary>
    public static Keyboard Create() => new(KeyReaders.Open());

    /// <summary>A keyboard that never reports a key, with the reason it cannot.</summary>
    public static Keyboard Unsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new(new NoKeyReader(reason));
    }

    /// <summary>Whether keys can be read on this system right now.</summary>
    public bool IsSupported
    {
        get
        {
            lock (_sync)
                return _reader.IsSupported;
        }
    }

    /// <summary>What the keyboard is read through, or why it cannot be read.</summary>
    public string Status
    {
        get
        {
            lock (_sync)
                return _reader.Status;
        }
    }

    /// <summary>Whether the key is held down at this moment.</summary>
    public bool IsDown(Key key)
    {
        lock (_sync)
        {
            if (_disposed || !_reader.IsSupported)
                return false;
            try
            {
                _reader.Refresh();
                return _reader.IsDown(key);
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                Fail(error);
                return false;
            }
        }
    }

    /// <summary>
    /// Calls back whenever the key goes down (<see langword="true"/>) or comes back up
    /// (<see langword="false"/>). A key already held when watching starts is not reported until it
    /// changes. The callback runs on the keyboard thread.
    /// </summary>
    /// <returns>A handle that stops watching when disposed.</returns>
    public IDisposable Watch(Key key, Action<bool> changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var watcher = new Watcher(this, key, changed, IsDownLocked(key));
            _watchers.Add(watcher);
            if (_poller is null && _reader.IsSupported)
            {
                _poller = new Thread(Poll) { IsBackground = true, Name = "QX keyboard" };
                _poller.Start();
            }
            return watcher;
        }
    }

    public void Dispose()
    {
        Thread? poller;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            _watchers.Clear();
            poller = _poller;
            _poller = null;
            _stopped.Set();
        }
        if (poller is not null && poller != Thread.CurrentThread)
            poller.Join(TimeSpan.FromSeconds(1));
        lock (_sync)
            _reader.Dispose();
    }

    void Remove(Watcher watcher)
    {
        lock (_sync)
            _watchers.Remove(watcher);
    }

    bool IsDownLocked(Key key)
    {
        if (!_reader.IsSupported)
            return false;
        try
        {
            _reader.Refresh();
            return _reader.IsDown(key);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Fail(error);
            return false;
        }
    }

    void Fail(Exception error)
    {
        IKeyReader failed = _reader;
        _reader = new NoKeyReader($"Keyboard reading stopped: {error.GetType().Name}: {error.Message}");
        try
        {
            failed.Dispose();
        }
        catch (Exception dispose_error) when (dispose_error is not OutOfMemoryException)
        {
        }
    }

    void Poll()
    {
        var changes = new List<(Action<bool> Changed, bool Down)>();
        while (!_stopped.Wait(PollInterval))
        {
            lock (_sync)
            {
                if (_disposed || _watchers.Count == 0 || !_reader.IsSupported)
                {
                    _poller = null;
                    return;
                }
                try
                {
                    _reader.Refresh();
                    foreach (Watcher watcher in _watchers)
                    {
                        bool down = _reader.IsDown(watcher.Key);
                        if (down == watcher.Down)
                            continue;
                        watcher.Down = down;
                        changes.Add((watcher.Changed, down));
                    }
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    Fail(error);
                    _poller = null;
                    return;
                }
            }
            foreach ((Action<bool> changed, bool down) in changes)
            {
                try
                {
                    changed(down);
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                }
            }
            changes.Clear();
        }
    }

    sealed class Watcher(Keyboard owner, Key key, Action<bool> changed, bool down) : IDisposable
    {
        int _disposed;

        public Key Key { get; } = key;

        public Action<bool> Changed { get; } = changed;

        public bool Down { get; set; } = down;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.Remove(this);
        }
    }
}
