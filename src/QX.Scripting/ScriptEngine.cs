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

public static class ScriptEngine
{
    private static ScriptOptions? _options;

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
    /// <param name="code">The script.</param>
    /// <param name="fileName">Its path, or a name for a script that has no file.</param>
    /// <param name="directory">
    /// The folder <c>#load</c> and <c>#r</c> paths resolve against when the script's own path
    /// does not settle them, normally the script library.
    /// </param>
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

    public static Task RunAsync(
        string code,
        ScriptGlobals globals,
        CancellationToken cancellationToken = default) =>
        Prepare(code).RunAsync(globals, cancellationToken);

    public static Task RunAsync(
        string code,
        ScriptGlobals globals,
        string fileName,
        CancellationToken cancellationToken = default) =>
        Prepare(code, fileName).RunAsync(globals, cancellationToken);

    public static ImmutableArray<Diagnostic> Compile(string code, string fileName = "script.csx", string? directory = null) =>
        Prepare(code, fileName, directory).Diagnostics;

    /// <summary>
    /// Compiles, without running, a small script that uses the everyday constructs, so the first
    /// real run does not pay for loading and JIT-compiling the compiler. Call it off the UI thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">The sample no longer compiles against the API.</exception>
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

public sealed class ScriptProgram
{
    private readonly ScriptRunner<object>? _runner;

    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    internal ScriptProgram(Script<object> script)
    {
        Diagnostics = script.Compile();
        if (!HasErrors)
            _runner = script.CreateDelegate();
    }

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
