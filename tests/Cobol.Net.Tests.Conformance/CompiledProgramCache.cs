// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CobolNet.Frontend;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE CONTENT-ADDRESSED COMPILED-PROGRAM CACHE of the Conformance runner (kb/Work PB985;
/// <c>docs/rearchitecture/DESIGN-test-build-ci.md</c> §3.12). EVERY compilation this assembly performs goes through
/// <see cref="Compile"/> (<c>CompiledProgramCacheDriftTests</c> forbids a direct <c>CompilerDriver.Compile</c>
/// anywhere else in it), so a re-gate after a TEST-ONLY fix — a probe template, a stale test-ref, a pinned
/// expectation — reuses every compiled program instead of recompiling ~7,900 of them. A hit still RUNS the program:
/// runtime behaviour is what the cases check, and only the compile is skipped.
/// <para><b>Correctness never depends on the cache.</b> The key is everything the compiler's output depends on,
/// in three parts: (1) the COMPILER'S OWN BITS — a hash of every non-framework assembly in the compiler's reference
/// closure, plus the exact shared-framework build, the current culture and the working directory
/// (<see cref="ToolFingerprint"/>); (2) EVERY <see cref="CompilerDriver.Options"/> property, rendered by
/// reflection so a new option is keyed the day it is added (<see cref="PrimaryKey"/>); (3) the compilation's
/// AMBIENT inputs as the compiler itself recorded them (<see cref="CompilerDriver.Result.Inputs"/> — each file it
/// read with its content hash, each path it probed and the answer, each environment variable a directive read),
/// re-verified on every lookup. A compilation that read the CLOCK (WHEN-COMPILED) is never stored, and neither is a
/// backend failure. Any compiler or runtime change therefore misses everywhere.</para>
/// <para><b>Switch.</b> <c>COBOLNET_COMPILE_CACHE=off</c> disables it; <c>=on</c> forces it on. Unset, it is ON —
/// except under CI (<c>CI</c> set), which runs COLD; <c>scripts/battery.sh</c> exports <c>off</c>, so the battery
/// is cold too (owner decision pending — PB985 obligation 4). <c>COBOLNET_COMPILE_CACHE_DIR</c> relocates the
/// store (default: <c>.cache/compiled-programs</c> under THIS worktree's root, git-ignored, so parallel worktrees
/// never share a store and a removed worktree takes its store with it); <c>COBOLNET_COMPILE_CACHE_MAX_MB</c> caps it
/// (default 4096; least-recently-used entries are evicted once per test process).</para>
/// </summary>
internal static class CompiledProgramCache
{
    internal const string SwitchVariable = "COBOLNET_COMPILE_CACHE";
    internal const string DirVariable = "COBOLNET_COMPILE_CACHE_DIR";
    internal const string MaxMbVariable = "COBOLNET_COMPILE_CACHE_MAX_MB";

    /// <summary>Bumped whenever the entry layout or the key recipe changes — an old store then misses whole.</summary>
    private const int FormatVersion = 2;   // 2: an entry carries the compilation's own output (kb/Work PB2116)

    /// <summary>Whether this process uses the cache (see the class remarks).</summary>
    internal static bool Enabled { get; } =
        ResolveEnabled(Environment.GetEnvironmentVariable(SwitchVariable), Environment.GetEnvironmentVariable("CI"));

    /// <summary>The store's root directory.</summary>
    internal static string Root { get; } =
        Environment.GetEnvironmentVariable(DirVariable) is { Length: > 0 } dir
            ? Path.GetFullPath(dir)
            : TestRepo.At(".cache", "compiled-programs");

    /// <summary>The switch's decision table: an explicit <c>on</c>/<c>off</c> wins; unset means ON outside CI.</summary>
    internal static bool ResolveEnabled(string? switchValue, string? ci) =>
        switchValue?.Trim().ToLowerInvariant() switch
        {
            "off" or "0" or "false" or "no" => false,
            "on" or "1" or "true" or "yes" => true,
            _ => string.IsNullOrEmpty(ci),
        };

    private static int s_hits, s_misses, s_stored, s_uncacheable;
    private static long s_compileTicks;

    /// <summary>Compile through the cache: a verified hit materializes the stored outputs where the compiler would
    /// have written them and returns the stored result; anything else compiles for real (and stores what it may).
    /// Result-equivalent to <see cref="CompilerDriver.Compile"/> for every caller, except that a hit's
    /// <see cref="CompilerDriver.Result.Inputs"/> is empty (the inputs were verified, not re-read).</summary>
    public static CompilerDriver.Result Compile(CompilerDriver.Options options)
    {
        if (!Enabled) return CompileCold(options);
        EnsureMaintained();
        return CompileThrough(options, Root).Result;
    }

    /// <summary>The cache proper, over an explicit store <paramref name="root"/> — <see cref="Compile"/> passes
    /// <see cref="Root"/>; the drift tests pass a private one, so they exercise the cache even where it is OFF.
    /// <c>Hit</c> says whether the result came from the store.</summary>
    internal static (CompilerDriver.Result Result, bool Hit) CompileThrough(CompilerDriver.Options options, string root)
    {
        if (!File.Exists(options.SourcePath)) return (CompileCold(options), false);
        string key;
        try { key = PrimaryKey(options); }
        catch (IOException) { return (CompileCold(options), false); }

        if (TryHit(root, key, options) is { } hit)
        {
            Interlocked.Increment(ref s_hits);
            return (hit, true);
        }

        Interlocked.Increment(ref s_misses);
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = CompileCold(options);
        Interlocked.Add(ref s_compileTicks, System.Diagnostics.Stopwatch.GetTimestamp() - start);
        if (Cacheability(result, options) is null) TryStore(root, key, options, result);
        else Interlocked.Increment(ref s_uncacheable);
        return (result, false);
    }

    /// <summary>The real compiler, never the cache — for a test that must observe a fresh compilation (the cache's
    /// own drift tests). ⛔ The ONLY direct <see cref="CompilerDriver.Compile"/> call in this assembly.</summary>
    public static CompilerDriver.Result CompileCold(CompilerDriver.Options options) => CompilerDriver.Compile(options);

    /// <summary>Place an in-memory program at a path that is STABLE across runs, so its compilation can hit. A
    /// harness that writes its source into a fresh random directory gets a different <c>SourcePath</c> — a
    /// different key — every run; here the directory is named by the content, so the same text lands at the same
    /// path every time, alone in its directory (a COPY search of the source directory therefore finds nothing but
    /// what the key recorded). The file keeps <paramref name="plannedPath"/>'s NAME. When the cache is OFF the text is
    /// written to <paramref name="plannedPath"/> itself, exactly as the harness always did, so a cold run keeps the
    /// historical layout. Returns the path to compile.
    /// <para>⛔ <paramref name="companions"/> (a copybook set the program COPYs from its own directory) are staged
    /// WITH the program, into the directory the program AND its companions name (kb/Work PB1755): they are part of
    /// the compile's input, and writing them into a directory named by the program text alone made every case
    /// that compiles the same program a second writer of the same file — the version matrix compiles one construct
    /// at four editions in parallel, and the loser read a half-written copybook ("being used by another process").
    /// Every file is placed write-if-absent through a temporary file and an atomic move, so a concurrent stager of
    /// the identical content never sees a partial file.</para></summary>
    public static string StageSource(string plannedPath, string sourceText,
        IReadOnlyDictionary<string, string>? companions = null)
    {
        var others = companions?.OrderBy(c => c.Key, StringComparer.Ordinal).ToList() ?? [];
        if (!Enabled)
        {
            File.WriteAllText(plannedPath, sourceText);
            foreach (var (name, text) in others) File.WriteAllText(Path.Combine(Path.GetDirectoryName(plannedPath)!, name), text);
            return plannedPath;
        }

        string fileName = Path.GetFileName(plannedPath);
        var identity = new StringBuilder(fileName).Append('\0').Append(sourceText);
        foreach (var (name, text) in others) identity.Append('\0').Append(name).Append('\0').Append(text);

        string dir = Path.Combine(Root, "src", Sha256Hex(Encoding.UTF8.GetBytes(identity.ToString()))[..32]);
        string path = Path.Combine(dir, fileName);
        Directory.CreateDirectory(dir);
        StageFile(path, sourceText);
        foreach (var (name, text) in others) StageFile(Path.Combine(dir, name), text);
        try { Directory.SetLastWriteTimeUtc(dir, DateTime.UtcNow); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return path;

        static void StageFile(string target, string text)
        {
            if (File.Exists(target)) return;
            string tmp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, text);
            try { File.Move(tmp, target); }
            catch (IOException) { TryDeleteFile(tmp); }   // a concurrent stager placed the identical file first
        }
    }

    // ───────────────────────────── the key ─────────────────────────────

    /// <summary>The COMPILER'S OWN BITS: a SHA-256 over every non-framework assembly in the compiler's reference
    /// closure (<see cref="CompilerClosure"/> — the compiler, the front end, the editions table, the runtime the
    /// generated program is compiled against and deployed with, Roslyn, ANTLR), the exact shared-framework build
    /// the compiler runs on (it is also the reference set Roslyn compiles against and the version the packaged
    /// runtimeconfig names), the current culture and the working directory. Computed once per process.</summary>
    internal static string ToolFingerprint => s_tool.Value;

    private static readonly Lazy<string> s_tool =
        new(() => FingerprintOf(CompilerClosure().Select(a => a.Location)), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The tool fingerprint over an explicit assembly set (the drift test flips a byte in a copy).</summary>
    internal static string FingerprintOf(IEnumerable<string> assemblyFiles)
    {
        var sb = new StringBuilder();
        foreach (string file in assemblyFiles.Order(StringComparer.OrdinalIgnoreCase))
            sb.Append(Path.GetFileName(file)).Append(' ').Append(Sha256Hex(File.ReadAllBytes(file))).Append('\n');
        sb.Append("framework ").Append(RuntimeInformation.FrameworkDescription).Append(' ')
          .Append(RuntimeEnvironment.GetRuntimeDirectory()).Append('\n');
        sb.Append("culture ").Append(CultureInfo.CurrentCulture.Name).Append('\n');
        sb.Append("cwd ").Append(Environment.CurrentDirectory).Append('\n');
        return Sha256Hex(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    /// <summary>The compiler's reference closure outside the shared framework: <c>Cobol.Net.Compiler</c> and every
    /// assembly it transitively references that is not a framework assembly. Anything the compiler can execute is
    /// in here — <c>CompilationInputsDriftTests</c> forbids it loading code by name (<c>Assembly.Load*</c>).</summary>
    internal static IReadOnlyList<Assembly> CompilerClosure()
    {
        string framework = Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory());
        var seen = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<Assembly>([typeof(CompilerDriver).Assembly]);
        while (queue.Count > 0)
        {
            var asm = queue.Dequeue();
            if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
            if (Path.GetFullPath(asm.Location).StartsWith(framework, StringComparison.OrdinalIgnoreCase)) continue;
            if (!seen.TryAdd(asm.GetName().Name!, asm)) continue;
            foreach (var reference in asm.GetReferencedAssemblies())
            {
                Assembly loaded;
                try { loaded = Assembly.Load(reference); }
                catch (FileNotFoundException) { continue; }   // an optional dependency never shipped — nothing to run
                queue.Enqueue(loaded);
            }
        }
        return [.. seen.Values];
    }

    /// <summary>Everything the compiler's output depends on that is known BEFORE compiling: the format, the tool,
    /// every option (reflected — see <see cref="KeyValue"/>) and the source's content hash. The ambient inputs
    /// only the compiler can name (copybooks, probes, environment) are recorded in the entry and verified on hit.</summary>
    internal static string PrimaryKey(CompilerDriver.Options options)
    {
        var sb = new StringBuilder();
        sb.Append("format ").Append(FormatVersion).Append('\n');
        sb.Append("tool ").Append(ToolFingerprint).Append('\n');
        foreach (var property in OptionProperties)
            sb.Append(property.Name).Append(' ').Append(KeyValue(property.Name, property.GetValue(options))).Append('\n');
        sb.Append("source ").Append(Sha256Hex(File.ReadAllBytes(options.SourcePath))).Append('\n');
        return Sha256Hex(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    /// <summary>Every public instance property of <see cref="CompilerDriver.Options"/>, in a stable order — the key
    /// covers an option the day it is added, with no list here to forget to extend.</summary>
    internal static IReadOnlyList<PropertyInfo> OptionProperties { get; } =
        [.. typeof(CompilerDriver.Options).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.Name != "EqualityContract")
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>One option's rendering in the key. <c>OutputPath</c> contributes only its FILE NAME (the assembly
    /// name): the output DIRECTORY does not reach the output — <c>CompiledProgramCacheDriftTests</c> compiles the
    /// same program into two directories and compares every byte — and keying it would make every harness's random
    /// run directory a miss. <c>SourcePath</c> is keyed in FULL: it reaches diagnostics and the COPY search. A
    /// property of a type with no rendering here THROWS, so a new kind of option cannot silently drop out.</summary>
    internal static string KeyValue(string name, object? value) => (name, value) switch
    {
        ("OutputPath", string path) => "name:" + Path.GetFileName(path),
        ("SourcePath", string path) => "path:" + Path.GetFullPath(path),
        (_, null) => "null",
        (_, string s) => "s:" + s,
        (_, bool or int or long or Enum) => value.GetType().Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture),
        (_, IEnumerable<string> list) => "[" + string.Join("\u001f", list) + "]",
        _ => throw new NotSupportedException(
            $"CompilerDriver.Options.{name} is a {value.GetType()}, which the compiled-program cache key has no "
            + "rendering for — add one to CompiledProgramCache.KeyValue (kb/Work PB985) before the option ships."),
    };

    // ───────────────────────────── cacheability ─────────────────────────────

    /// <summary>Why <paramref name="result"/> must NOT be stored, or null when it may be.</summary>
    internal static string? Cacheability(CompilerDriver.Result result, CompilerDriver.Options options)
    {
        if (result.Status is not (CompilerDriver.Outcome.Success or CompilerDriver.Outcome.FrontendError
                or CompilerDriver.Outcome.BindError))
            return $"outcome {result.Status} is not cached";
        if (result.Inputs.ReadsCompilationTime)
            return "the compilation read the clock (WHEN-COMPILED) — its output is a function of WHEN it ran";
        string outDir = OutputDirectory(options);
        foreach (string file in result.OutputFiles)
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(file)), outDir, StringComparison.OrdinalIgnoreCase))
                return $"output {file} is outside the output directory";
        if (result.Success && !options.CheckOnly && ForeignReference(OutputDll(options)) is { } foreign)
            return $"the program references {foreign}, an assembly outside the keyed compiler closure";
        return null;
    }

    /// <summary>The first assembly the produced program references that the key does NOT cover, or null. Roslyn
    /// compiles the program against every trusted-platform assembly of the test host — which includes the TEST
    /// assemblies, whose bits are deliberately not keyed (a test-only fix must hit). Binding to one of them would be
    /// the only way they could shape the output, and it would show up here as an assembly reference.</summary>
    internal static string? ForeignReference(string dllPath)
    {
        var allowed = s_closureNames.Value;
        string framework = RuntimeEnvironment.GetRuntimeDirectory();
        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        foreach (var handle in md.AssemblyReferences)
        {
            string name = md.GetString(md.GetAssemblyReference(handle).Name);
            if (allowed.Contains(name) || File.Exists(Path.Combine(framework, name + ".dll"))) continue;
            return name;
        }
        return null;
    }

    private static readonly Lazy<HashSet<string>> s_closureNames = new(
        () => new HashSet<string>(CompilerClosure().Select(a => a.GetName().Name!), StringComparer.OrdinalIgnoreCase),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static string OutputDll(CompilerDriver.Options options) =>
        options.OutputPath ?? Path.ChangeExtension(options.SourcePath, ".dll");   // CompilerDriver.Compile's default

    private static string OutputDirectory(CompilerDriver.Options options) =>
        Path.GetDirectoryName(Path.GetFullPath(OutputDll(options)))!;

    // ───────────────────────────── the store ─────────────────────────────

    /// <summary>A stored compilation. <paramref name="CompileOutput"/> is what the compilation itself transferred
    /// (the DISPLAY directive's lines, ISO §7.3.12.4 1)): part of the result, so a hit replays it.</summary>
    private sealed record Entry(int Format, string Status, List<string> Errors, List<string> Warnings,
        string? GeneratedSource, List<StoredFile> Files, List<ReadInput> Reads, List<InputProbe> Probes,
        List<EnvironmentRead> Environment, List<Frontend.Diagnostics.CompileOutputLine> CompileOutput);

    private sealed record StoredFile(string Name, string Blob);

    private sealed record ReadInput(string Path, string Sha256);

    private static string EntryPath(string root, string key) => Path.Combine(root, "entries", key[..2], key + ".json");

    private static string BlobPath(string root, string sha) => Path.Combine(root, "blobs", sha[..2], sha);

    private static CompilerDriver.Result? TryHit(string root, string key, CompilerDriver.Options options)
    {
        string path = EntryPath(root, key);
        Entry? entry;
        try
        {
            if (!File.Exists(path)) return null;
            entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return null; }
        if (entry is null || entry.Format != FormatVersion) return null;

        try
        {
            // Re-verify every ambient input the compiler recorded: a copybook edited, a shadowing copybook that
            // appeared earlier in the search order, a directive's environment variable that changed — each a miss.
            foreach (var read in entry.Reads)
                if (!File.Exists(read.Path) || Sha256Hex(File.ReadAllBytes(read.Path)) != read.Sha256) return null;
            foreach (var probe in entry.Probes)
                if ((probe.IsDirectory ? Directory.Exists(probe.Path) : File.Exists(probe.Path)) != probe.Exists) return null;
            foreach (var env in entry.Environment)
                if (System.Environment.GetEnvironmentVariable(env.Name) != env.Value) return null;

            string outDir = OutputDirectory(options);
            var written = new List<string>();
            if (entry.Files.Count > 0) Directory.CreateDirectory(outDir);
            foreach (var file in entry.Files)
            {
                string dest = Path.Combine(outDir, file.Name);
                using (var blob = File.OpenRead(BlobPath(root, file.Blob)))
                using (var inflate = new BrotliStream(blob, CompressionMode.Decompress))
                using (var output = File.Create(dest))
                    inflate.CopyTo(output);
                written.Add(dest);
            }
            try { File.SetLastWriteTimeUtc(path, DateTime.UtcNow); } catch (IOException) { }   // the LRU clock

            var status = Enum.Parse<CompilerDriver.Outcome>(entry.Status);
            bool produced = status == CompilerDriver.Outcome.Success && !options.CheckOnly;
            return new CompilerDriver.Result(status, produced ? OutputDll(options) : "",
                entry.GeneratedSource is { } cs ? Path.Combine(outDir, cs) : null, entry.Errors, entry.Warnings)
            {
                OutputFiles = written,
                CompileOutput = entry.CompileOutput,
            };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return null;   // an evicted blob, a torn entry: compile for real (it overwrites anything half-written)
        }
    }

    private static void TryStore(string root, string key, CompilerDriver.Options options, CompilerDriver.Result result)
    {
        try
        {
            var files = new List<StoredFile>();
            foreach (string file in result.OutputFiles)
            {
                byte[] bytes = File.ReadAllBytes(file);
                string sha = Sha256Hex(bytes);
                string blob = BlobPath(root, sha);
                if (!File.Exists(blob))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(blob)!);
                    string tmp = blob + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    using (var output = File.Create(tmp))
                    using (var deflate = new BrotliStream(output, CompressionLevel.Fastest))
                        deflate.Write(bytes);
                    try { File.Move(tmp, blob); }
                    catch (IOException) { TryDeleteFile(tmp); }   // a concurrent writer stored the same content
                }
                files.Add(new StoredFile(Path.GetFileName(file), sha));
            }

            var entry = new Entry(FormatVersion, result.Status.ToString(), [.. result.Errors], [.. result.Warnings],
                result.GeneratedCsPath is { } cs ? Path.GetFileName(cs) : null, files,
                [.. result.Inputs.FilesRead.Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(p => new ReadInput(p, Sha256Hex(File.ReadAllBytes(p))))],
                [.. result.Inputs.Probes.Distinct()],
                [.. result.Inputs.EnvironmentReads.Distinct()],
                [.. result.CompileOutput]);
            string path = EntryPath(root, key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tmpEntry = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmpEntry, JsonSerializer.Serialize(entry));
            try { File.Move(tmpEntry, path, overwrite: true); Interlocked.Increment(ref s_stored); }
            catch (IOException) { TryDeleteFile(tmpEntry); }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* a store is best-effort */ }
    }

    // ───────────────────────────── maintenance ─────────────────────────────

    private static readonly Lazy<bool> s_maintained = new(Maintain, LazyThreadSafetyMode.ExecutionAndPublication);

    private static void EnsureMaintained() => _ = s_maintained.Value;

    /// <summary>Once per test process: enforce the size cap (evict the least-recently-used half of the entries,
    /// then every blob no surviving entry names), drop staged sources untouched for a week, and register the
    /// hit/miss tally for the end of the run. Serialized across processes by an exclusive lock file; a process that
    /// cannot take it leaves the maintenance to the one that did.</summary>
    private static bool Maintain()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => WriteStats();
        try
        {
            Directory.CreateDirectory(Root);
            using var gate = new FileStream(Path.Combine(Root, "maintain.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            long cap = (long.TryParse(Environment.GetEnvironmentVariable(MaxMbVariable), out long mb) && mb > 0 ? mb : 4096)
                       * 1024 * 1024;
            var blobsDir = new DirectoryInfo(Path.Combine(Root, "blobs"));
            var blobs = blobsDir.Exists ? blobsDir.EnumerateFiles("*", SearchOption.AllDirectories).ToList() : [];
            if (blobs.Sum(f => f.Length) > cap)
            {
                var entriesDir = new DirectoryInfo(Path.Combine(Root, "entries"));
                var entries = entriesDir.EnumerateFiles("*.json", SearchOption.AllDirectories)
                    .OrderBy(f => f.LastWriteTimeUtc).ToList();
                foreach (var stale in entries.Take(entries.Count / 2)) TryDeleteFile(stale.FullName);
                var live = new HashSet<string>(StringComparer.Ordinal);
                foreach (var survivor in entries.Skip(entries.Count / 2))
                {
                    try
                    {
                        if (JsonSerializer.Deserialize<Entry>(File.ReadAllText(survivor.FullName)) is { } e)
                            live.UnionWith(e.Files.Select(f => f.Blob));
                    }
                    catch (Exception ex) when (ex is IOException or JsonException) { }
                }
                // A blob younger than ten minutes may belong to an entry a concurrent writer has not published yet.
                foreach (var blob in blobs)
                    if (!live.Contains(blob.Name) && blob.LastWriteTimeUtc < DateTime.UtcNow.AddMinutes(-10))
                        TryDeleteFile(blob.FullName);
            }
            var staged = new DirectoryInfo(Path.Combine(Root, "src"));
            if (staged.Exists)
                foreach (var dir in staged.EnumerateDirectories().Where(d => d.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7)))
                    try { dir.Delete(recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* another process maintains */ }
        return true;
    }

    /// <summary>Append this process's tally to <c>stats.log</c> — the measurement PB985 obligation 5 reads.</summary>
    private static void WriteStats()
    {
        try
        {
            File.AppendAllText(Path.Combine(Root, "stats.log"), string.Create(CultureInfo.InvariantCulture,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} pid={Environment.ProcessId} hits={s_hits} misses={s_misses} "
                + $"stored={s_stored} uncacheable={s_uncacheable} "
                + $"miss-compile-s={s_compileTicks / (double)System.Diagnostics.Stopwatch.Frequency:F1}\n"));
        }
        catch (IOException) { }
    }

    internal static string Sha256Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
