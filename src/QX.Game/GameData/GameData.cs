using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Qx.Game;

internal sealed record GameDataState(
    long Revision,
    long LoadGeneration,
    string? WebHost,
    bool Loaded,
    FurniData? Furni,
    ProductData? Products,
    ExternalTexts? Texts,
    ExternalVariables? Variables);

internal interface IGameDataTransport
{
    Task<IReadOnlyDictionary<string, string>> LoadHashesAsync(
        string web_host,
        CancellationToken cancellation_token);

    Task<string> FetchAsync(
        string web_host,
        string path,
        string hash,
        string cache_name,
        string cache_key,
        CancellationToken cancellation_token);
}

/// <summary>
/// Provides the hotel's game data files: furni data, product data, external texts and external
/// variables.
/// </summary>
/// <remarks>
/// The files are downloaded from the hotel's web host under <c>/gamedata/</c>, versioned by the
/// hashes the host publishes, and cached on disk under <see cref="StoragePaths.Cache"/>. Every
/// data property is <see langword="null"/> until a load for the current host completes.
/// </remarks>
public sealed class GameData
{
    private static readonly Dictionary<string, string> web_hosts = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["game-us.habbo.com"] = "www.habbo.com",
        ["game-es.habbo.com"] = "www.habbo.es",
        ["game-fi.habbo.com"] = "www.habbo.fi",
        ["game-it.habbo.com"] = "www.habbo.it",
        ["game-nl.habbo.com"] = "www.habbo.nl",
        ["game-de.habbo.com"] = "www.habbo.de",
        ["game-fr.habbo.com"] = "www.habbo.fr",
        ["game-br.habbo.com"] = "www.habbo.com.br",
        ["game-tr.habbo.com"] = "www.habbo.com.tr",
        ["game-s2.habbo.com"] = "sandbox.habbo.com"
    };

    private readonly object state_sync = new();
    private readonly IGameDataTransport transport;
    private GameDataState state = new(0, 0, null, false, null, null, null, null);
    private GameDataLoadOperation? active_load;
    private long load_generation;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameData"/> class that downloads over HTTPS
    /// with a 30 second timeout and caches files on disk.
    /// </summary>
    public GameData()
        : this(new DefaultGameDataTransport())
    {
    }

    internal GameData(IGameDataTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>Gets the furni data, or <see langword="null"/> when game data is not loaded.</summary>
    public FurniData? Furni => State.Furni;
    /// <summary>Gets the product data, or <see langword="null"/> when game data is not loaded.</summary>
    public ProductData? Products => State.Products;
    /// <summary>Gets the external texts, or <see langword="null"/> when game data is not loaded.</summary>
    public ExternalTexts? Texts => State.Texts;
    /// <summary>
    /// Gets the external variables, or <see langword="null"/> when game data is not loaded or the
    /// variables file failed to load.
    /// </summary>
    /// <remarks>
    /// A failure to load the variables does not fail the whole load; it is reported through
    /// <see cref="Status"/> and the other files are still published.
    /// </remarks>
    public ExternalVariables? Variables => State.Variables;
    /// <summary>Gets whether game data for the current web host has finished loading.</summary>
    public bool IsLoaded => State.Loaded;

    internal GameDataState State => Volatile.Read(ref state);

    /// <summary>Occurs when a load completes and its data is published.</summary>
    /// <remarks>
    /// Raised on the thread that finished the load. Handlers are skipped once a newer load has
    /// replaced the one that completed, and exceptions thrown by handlers are ignored.
    /// </remarks>
    public event Action? Loaded;
    /// <summary>Occurs when the loader reports progress or an error as a human-readable message.</summary>
    /// <remarks>
    /// Messages report the start of a load, an unavailable variables file, the final counts and
    /// load failures. Exceptions thrown by handlers are ignored.
    /// </remarks>
    public event Action<string>? Status;

    /// <summary>Gets the web host that serves game data for a game server host.</summary>
    /// <param name="gameHost">The game server host name, such as <c>game-us.habbo.com</c>, matched case-insensitively.</param>
    /// <returns>The matching web host, such as <c>www.habbo.com</c>, or <c>www.habbo.com</c> when the host is not known.</returns>
    public static string WebHostFor(string gameHost) =>
        web_hosts.GetValueOrDefault(gameHost, "www.habbo.com");

    /// <summary>Loads the game data for a game server host.</summary>
    /// <remarks>
    /// <para>
    /// Returns at once when data for the same web host is already loaded, and joins a load that
    /// is already running for that host. A request for a different host cancels the running load,
    /// clears the current data and starts over.
    /// </para>
    /// <para>
    /// The returned task completes when the load ends, whether it succeeded or not; a failure is
    /// reported through <see cref="Status"/> and leaves <see cref="IsLoaded"/>
    /// <see langword="false"/>. Canceling <paramref name="cancellationToken"/> only stops the wait
    /// and does not stop the load.
    /// </para>
    /// </remarks>
    /// <param name="gameHost">The game server host name, resolved with <see cref="WebHostFor"/>.</param>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes when the load has ended.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    public async Task LoadAsync(
        string gameHost,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string web_host = WebHostFor(gameHost);
        GameDataLoadOperation operation;
        GameDataLoadOperation? superseded = null;
        bool start = false;
        lock (state_sync)
        {
            GameDataState current = state;
            if (current.Loaded &&
                string.Equals(current.WebHost, web_host, StringComparison.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
            if (active_load is { } active &&
                string.Equals(active.WebHost, web_host, StringComparison.OrdinalIgnoreCase))
            {
                operation = active;
            }
            else
            {
                superseded = active_load;
                operation = new GameDataLoadOperation(
                    web_host,
                    checked(++load_generation));
                active_load = operation;
                Volatile.Write(ref state, new GameDataState(
                    checked(current.Revision + 1),
                    operation.Generation,
                    web_host,
                    false,
                    null,
                    null,
                    null,
                    null));
                start = true;
            }
        }
        Cancel(superseded);
        if (start)
            _ = RunLoadAsync(operation);
        try
        {
            await operation.Completion.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private async Task RunLoadAsync(GameDataLoadOperation operation)
    {
        try
        {
            PublishStatus(operation, $"loading game data from {operation.WebHost} ...");
            IReadOnlyDictionary<string, string> hashes = await transport
                .LoadHashesAsync(operation.WebHost, operation.Cancellation.Token)
                .ConfigureAwait(false);
            operation.Cancellation.Token.ThrowIfCancellationRequested();

            string furni_hash = ValueOrDefault(hashes, "furnidata", "1");
            string product_hash = ValueOrDefault(hashes, "productdata", "1");
            string texts_hash = ValueOrDefault(hashes, "external_texts", "1");
            string variables_hash = ValueOrDefault(hashes, "external_variables", "1");

            Task<string> furni_request = transport.FetchAsync(
                operation.WebHost,
                "furnidata_json",
                furni_hash,
                "furnidata",
                furni_hash,
                operation.Cancellation.Token);
            Task<string> product_request = transport.FetchAsync(
                operation.WebHost,
                "productdata_json",
                product_hash,
                "productdata",
                product_hash,
                operation.Cancellation.Token);
            Task<string> texts_request = transport.FetchAsync(
                operation.WebHost,
                "external_flash_texts",
                texts_hash,
                "external_texts",
                texts_hash,
                operation.Cancellation.Token);
            Task<VariableLoadResult> variables_request = LoadVariablesAsync(
                operation,
                variables_hash);

            await Task.WhenAll(furni_request, product_request, texts_request)
                .ConfigureAwait(false);
            FurniData furni = FurniData.LoadJson(await furni_request.ConfigureAwait(false));
            ProductData products = ProductData.LoadJson(
                await product_request.ConfigureAwait(false));
            ExternalTexts texts = ExternalTexts.Load(
                await texts_request.ConfigureAwait(false));
            VariableLoadResult variables = await variables_request.ConfigureAwait(false);
            operation.Cancellation.Token.ThrowIfCancellationRequested();

            if (variables.Error is { } variables_error)
            {
                PublishStatus(
                    operation,
                    $"external variables unavailable: {variables_error.Message}");
            }

            bool committed;
            lock (state_sync)
            {
                committed = ReferenceEquals(active_load, operation) &&
                    state.LoadGeneration == operation.Generation &&
                    string.Equals(
                        state.WebHost,
                        operation.WebHost,
                        StringComparison.OrdinalIgnoreCase);
                if (committed)
                {
                    GameDataState current = state;
                    Volatile.Write(ref state, new GameDataState(
                        checked(current.Revision + 1),
                        operation.Generation,
                        operation.WebHost,
                        true,
                        furni,
                        products,
                        texts,
                        variables.Value));
                    active_load = null;
                }
            }
            if (!committed)
                return;

            string variable_count = variables.Value is null
                ? "no variables"
                : $"{variables.Value.Count} variables";
            PublishStatus(
                operation,
                $"game data ready: {furni.FloorItems.Count + furni.WallItems.Count} furni, " +
                $"{products.Count} products, {texts.Count} texts, {variable_count}");
            PublishLoaded(operation);
        }
        catch (OperationCanceledException) when (operation.Cancellation.IsCancellationRequested)
        {
            ClearActive(operation);
        }
        catch (Exception error)
        {
            bool current = ClearActive(operation);
            if (current)
                PublishStatus(operation, $"game data load failed: {error.Message}");
        }
        finally
        {
            operation.Completion.TrySetResult();
            operation.Cancellation.Dispose();
        }
    }

    private async Task<VariableLoadResult> LoadVariablesAsync(
        GameDataLoadOperation operation,
        string hash)
    {
        try
        {
            string content = await transport.FetchAsync(
                operation.WebHost,
                "external_variables",
                hash,
                "external_variables",
                hash,
                operation.Cancellation.Token).ConfigureAwait(false);
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["url.prefix"] = $"https://{operation.WebHost}"
            };
            return new VariableLoadResult(
                ExternalVariables.Load(content, isSecure: true, arguments),
                null);
        }
        catch (OperationCanceledException) when (operation.Cancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            return new VariableLoadResult(null, error);
        }
    }

    private bool ClearActive(GameDataLoadOperation operation)
    {
        lock (state_sync)
        {
            if (!ReferenceEquals(active_load, operation))
                return false;
            active_load = null;
            return true;
        }
    }

    private bool Current(GameDataLoadOperation operation)
    {
        GameDataState current = State;
        return current.LoadGeneration == operation.Generation &&
            string.Equals(
                current.WebHost,
                operation.WebHost,
                StringComparison.OrdinalIgnoreCase);
    }

    private void PublishStatus(GameDataLoadOperation operation, string message)
    {
        Action<string>? listeners = Status;
        if (listeners is null)
            return;
        foreach (Action<string> listener in listeners.GetInvocationList().Cast<Action<string>>())
        {
            if (!Current(operation))
                return;
            try
            {
                listener(message);
            }
            catch
            {
            }
        }
    }

    private void PublishLoaded(GameDataLoadOperation operation)
    {
        Action? listeners = Loaded;
        if (listeners is null)
            return;
        foreach (Action listener in listeners.GetInvocationList().Cast<Action>())
        {
            if (!Current(operation) || !State.Loaded)
                return;
            try
            {
                listener();
            }
            catch
            {
            }
        }
    }

    private static string ValueOrDefault(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback) => values.TryGetValue(key, out string? value)
        ? value
        : fallback;

    private static void Cancel(GameDataLoadOperation? operation)
    {
        if (operation is null)
            return;
        try
        {
            operation.Cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed class GameDataLoadOperation(string web_host, long generation)
    {
        public string WebHost { get; } = web_host;
        public long Generation { get; } = generation;
        public CancellationTokenSource Cancellation { get; } = new();
        public TaskCompletionSource Completion { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record VariableLoadResult(
        ExternalVariables? Value,
        Exception? Error);

    private sealed class DefaultGameDataTransport : IGameDataTransport
    {
        private static readonly HttpClient http = CreateClient();
        private readonly string cache_root = Path.Combine(StoragePaths.Cache, "gamedata");

        public async Task<IReadOnlyDictionary<string, string>> LoadHashesAsync(
            string web_host,
            CancellationToken cancellation_token)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string json = await http.GetStringAsync(
                $"https://{web_host}/gamedata/hashes2",
                cancellation_token).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("hashes", out JsonElement hashes))
                return result;
            foreach (JsonElement entry in hashes.EnumerateArray())
            {
                if (entry.TryGetProperty("name", out JsonElement name) &&
                    entry.TryGetProperty("hash", out JsonElement hash))
                {
                    result[name.GetString() ?? ""] = hash.GetString() ?? "1";
                }
            }
            return result;
        }

        public async Task<string> FetchAsync(
            string web_host,
            string path,
            string hash,
            string cache_name,
            string cache_key,
            CancellationToken cancellation_token)
        {
            string directory = Path.Combine(cache_root, web_host);
            string file = Path.Combine(directory, $"{cache_name}_{cache_key}");
            if (File.Exists(file))
                return await File.ReadAllTextAsync(file, cancellation_token).ConfigureAwait(false);

            string content = await http.GetStringAsync(
                $"https://{web_host}/gamedata/{path}/{hash}",
                cancellation_token).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(file, content, cancellation_token)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            return content;
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("User-Agent", "QX");
            return client;
        }
    }
}
