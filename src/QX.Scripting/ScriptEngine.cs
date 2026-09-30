using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Qx.Game;
using Qx.Interception;
using Qx.Messages;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

/// <summary>
/// Provides compilation and execution of QX scripts against <see cref="ScriptGlobals"/>.
/// </summary>
public static class ScriptEngine
{
    private static ScriptOptions? _options;

    /// <summary>Gets the assemblies that scripts are compiled against.</summary>
    /// <remarks>
    /// An assembly without a physical file on disk, such as one bundled into a single-file host
    /// that was not extracted, is not passed to the compiler.
    /// </remarks>
    public static IReadOnlyList<Assembly> ReferenceAssemblies { get; } =
    [
        typeof(ScriptGlobals).Assembly,
        typeof(IInterceptor).Assembly,
        typeof(RoomManager).Assembly,
        typeof(IPacket).Assembly,
        typeof(Qx.Protocol.MessageKey).Assembly,
        typeof(RoomEntryInfo).Assembly,
        typeof(Qx.Platform.Keyboard).Assembly
    ];

    /// <summary>Gets the namespaces every script imports without a using directive.</summary>
    public static IReadOnlyList<string> Imports { get; } =
    [
        "System",
        "System.Collections.Generic",
        "System.Collections.Concurrent",
        "System.Globalization",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Threading",
        "System.Threading.Tasks",
        "Qx",
        "Qx.Messages",
        "Qx.Interception",
        "Qx.Game",
        "Qx.Model",
        "Qx.Model.Bots",
        "Qx.Model.Crafting",
        "Qx.Model.Figures",
        "Qx.Model.Forums",
        "Qx.Model.Marketplace",
        "Qx.Model.Messages.Incoming",
        "Qx.Model.Messages.Outgoing",
        "Qx.Model.Polls",
        "Qx.Model.Quests",
        "Qx.Model.Subscriptions",
        "Qx.Model.Wired",
        "Qx.Game.Snapshots",
        "Qx.Platform",
        "Qx.Scripting"
    ];

    private static ScriptOptions Options => _options ??= Build();

    private static ScriptOptions Build() =>
        ScriptOptions.Default
            .WithReferences(ReferenceAssemblies.Where(HasPhysicalMetadata))
            .WithImports(Imports)
            .WithEmitDebugInformation(true)
            .WithOptimizationLevel(OptimizationLevel.Debug);

    [UnconditionalSuppressMessage(
        "SingleFile",
        "IL3002",
        Justification = "Scripting references are admitted only when the host extracted managed assemblies.")]
    private static bool HasPhysicalMetadata(Assembly assembly) =>
        File.Exists(assembly.ManifestModule.FullyQualifiedName);

    /// <summary>
    /// Compiles a script, making every loop in it and in the files it loads stop with the run.
    /// </summary>
    /// <remarks>
    /// Every <c>while</c>, <c>do</c>, <c>for</c> and <c>foreach</c> body starts with a
    /// cancellation check, and calls to every overload of <c>Task.Delay</c> and
    /// <c>Thread.Sleep</c> are redirected to <see cref="ScriptExecutionContext"/> so they end
    /// when the run is stopped. Compile errors are reported through
    /// <see cref="ScriptProgram.Diagnostics"/> rather than thrown.
    /// </remarks>
    /// <param name="code">The script source code.</param>
    /// <param name="fileName">The script path, or a name for a script that has no file.</param>
    /// <param name="directory">
    /// The folder <c>#load</c> and <c>#r</c> paths resolve against when the script's own path
    /// does not settle them, normally the script library.
    /// </param>
    /// <returns>The compiled program, which carries the compiler diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is <see langword="null"/>, empty or whitespace.</exception>
    public static ScriptProgram Prepare(string code, string fileName = "script.csx", string? directory = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        ScriptOptions options = Options
            .WithFilePath(fileName)
            .WithFileEncoding(Encoding.UTF8)
            .WithSourceResolver(new ScriptSourceResolver(directory))
            .WithMetadataResolver(ScriptMetadataResolver.Default.WithBaseDirectory(directory));
        Script<object> script = CSharpScript.Create(code, options, typeof(ScriptGlobals));
        ScriptRewrite rewrite = ScriptCancellationRewriter.Rewrite(script);
        if (rewrite.Loaded.Count > 0 || !string.Equals(code, rewrite.Main, StringComparison.Ordinal))
        {
            script = CSharpScript.Create(
                rewrite.Main,
                options.WithSourceResolver(new ScriptSourceResolver(directory, rewrite.Loaded)),
                typeof(ScriptGlobals));
        }
        return new ScriptProgram(script);
    }

    /// <summary>
    /// Compiles and runs a script under the file name <c>script.csx</c>.
    /// </summary>
    /// <param name="code">The script source code.</param>
    /// <param name="globals">The globals the script runs against.</param>
    /// <param name="cancellationToken">
    /// The token that stops the script, or <see langword="default"/> to use the token the globals
    /// were created with.
    /// </param>
    /// <returns>A task that completes when the script body has finished.</returns>
    /// <exception cref="CompilationErrorException">Thrown when the script has compile errors.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped.</exception>
    public static Task RunAsync(
        string code,
        ScriptGlobals globals,
        CancellationToken cancellationToken = default) =>
        Prepare(code).RunAsync(globals, cancellationToken);

    /// <summary>
    /// Compiles and runs a script under the specified file name.
    /// </summary>
    /// <param name="code">The script source code.</param>
    /// <param name="globals">The globals the script runs against.</param>
    /// <param name="fileName">The script path, or a name for a script that has no file.</param>
    /// <param name="cancellationToken">
    /// The token that stops the script, or <see langword="default"/> to use the token the globals
    /// were created with.
    /// </param>
    /// <returns>A task that completes when the script body has finished.</returns>
    /// <exception cref="CompilationErrorException">Thrown when the script has compile errors.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped.</exception>
    public static Task RunAsync(
        string code,
        ScriptGlobals globals,
        string fileName,
        CancellationToken cancellationToken = default) =>
        Prepare(code, fileName).RunAsync(globals, cancellationToken);

    /// <summary>
    /// Compiles a script without running it.
    /// </summary>
    /// <param name="code">The script source code.</param>
    /// <param name="fileName">The script path, or a name for a script that has no file.</param>
    /// <param name="directory">
    /// The folder <c>#load</c> and <c>#r</c> paths resolve against when the script's own path
    /// does not settle them, or <see langword="null"/>.
    /// </param>
    /// <returns>The compiler diagnostics, including warnings; empty when the script compiles cleanly.</returns>
    public static ImmutableArray<Diagnostic> Compile(string code, string fileName = "script.csx", string? directory = null) =>
        Prepare(code, fileName, directory).Diagnostics;

    /// <summary>
    /// Compiles, without running, a small script that uses the everyday constructs, so the first
    /// real run does not pay for loading and JIT compiling the compiler.
    /// </summary>
    /// <remarks>Call it off the UI thread.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when the sample no longer compiles against the API.</exception>
    public static void WarmUp()
    {
        ScriptProgram program = Prepare(
            """
            var items = FloorItems.Where(item => item.Id > 0).ToList();
            await Delay(0);
            Log(items.Count);
            """,
            "warm-up.csx");
        if (program.Diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) is { } error)
            throw new InvalidOperationException($"The compiler warm-up script no longer compiles: {error}");
    }
}

/// <summary>
/// Represents a compiled script, created by <see cref="ScriptEngine.Prepare(string, string, string?)"/>.
/// </summary>
public sealed class ScriptProgram
{
    private readonly ScriptRunner<object>? _runner;

    /// <summary>Gets the diagnostics the compiler reported, including warnings.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    /// <summary>Gets whether any diagnostic is an error, in which case the program cannot run.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    internal ScriptProgram(Script<object> script)
    {
        Diagnostics = script.Compile();
        if (!HasErrors)
            _runner = script.CreateDelegate();
    }

    /// <summary>
    /// Runs the compiled script against the specified globals.
    /// </summary>
    /// <remarks>
    /// The token becomes the ambient script token for the run, which <see cref="ScriptGlobals.Ct"/>,
    /// the loop checks and the redirected delays inside the script observe.
    /// </remarks>
    /// <param name="globals">The globals the script runs against.</param>
    /// <param name="cancellationToken">
    /// The token that stops the script, or <see langword="default"/> to use the token the globals
    /// were created with.
    /// </param>
    /// <returns>A task that completes when the script body has finished.</returns>
    /// <exception cref="CompilationErrorException">Thrown when the script has compile errors.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the token was canceled while the script ran or by the time its body returned.
    /// </exception>
    public async Task RunAsync(ScriptGlobals globals, CancellationToken cancellationToken = default)
    {
        if (_runner is null)
            throw new CompilationErrorException("Script compilation failed.", Diagnostics);
        CancellationToken scriptCancellation = cancellationToken.CanBeCanceled
            ? cancellationToken
            : globals.BaseCancellationToken;
        using IDisposable scope = ScriptExecutionContext.Enter(scriptCancellation);
        _ = await _runner(globals, scriptCancellation).ConfigureAwait(false);
        scriptCancellation.ThrowIfCancellationRequested();
    }
}
