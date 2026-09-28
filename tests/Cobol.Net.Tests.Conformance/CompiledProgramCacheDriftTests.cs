// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE KEY OF <see cref="CompiledProgramCache"/> IS COMPLETE (kb/Work PB985 obligation 2;
/// <c>DESIGN-test-build-ci.md</c> §3.12). A cache whose key omits one compiler input returns a STALE program — a
/// green verdict about code that no longer exists — so every input axis is flipped here and each flip must MISS:
/// every <see cref="CompilerDriver.Options"/> property (found by reflection, so a new option is covered the day it
/// ships), the source text, a copybook's text, a copybook that newly SHADOWS the one found, a directive's
/// environment variable, the compiler's own bits, and the clock (never stored). The one input deliberately NOT keyed
/// — the output DIRECTORY — is proven not to reach the output. The cache's scope is proven too: no compile in this
/// assembly bypasses it, and no ambient read in the compiler bypasses the <c>CompilationInputs</c> record.
/// These run against a PRIVATE store, so they exercise the cache even where it is switched off (CI, the battery).
/// </summary>
public sealed class CompiledProgramCacheDriftTests : IDisposable
{
    private readonly string _dir = CutRunner.NewTempDir("pb985");

    private string Store => Path.Combine(_dir, "store");

    public void Dispose() => CutRunner.TryDelete(_dir);

    private string Write(string relative, string text)
    {
        string path = Path.Combine(_dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private CompilerDriver.Options Options(string src, string runDir = "run", int edition = 2023,
        IReadOnlyList<string>? copyPaths = null) =>
        new(src, Path.Combine(_dir, runDir, "prog.dll"), DialectLevel: edition, CopyPaths: copyPaths);

    private (CompilerDriver.Result Result, bool Hit) Through(CompilerDriver.Options o) =>
        CompiledProgramCache.CompileThrough(o, Store);

    private const string Plain = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB985PLAIN.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-A PIC X(5) VALUE "PLAIN".
               PROCEDURE DIVISION.
                   DISPLAY WS-A
                   STOP RUN.
        """;

    private const string WithCopy = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB985COPY.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               COPY PB985CPY.
               PROCEDURE DIVISION.
                   DISPLAY WS-A
                   STOP RUN.
        """;

    private static string Copybook(string value) => $"""
               01 WS-A PIC X(5) VALUE "{value}".
        """;

    [Fact]
    public void ASecondCompile_Hits_AndTheMaterializedProgramRuns()
    {
        string src = Write("src/prog.cob", Plain);
        var first = Through(Options(src));
        Assert.True(first.Result.Success, string.Join("\n", first.Result.Errors));
        Assert.False(first.Hit);

        var second = Through(Options(src, runDir: "run2"));   // a DIFFERENT run directory: still a hit
        Assert.True(second.Hit, "an unchanged program must hit");
        Assert.True(second.Result.Success);
        Assert.Equal(File.ReadAllBytes(first.Result.OutputDll), File.ReadAllBytes(second.Result.OutputDll));
        var (ok, stdout, detail) = CutRunner.Run(second.Result.OutputDll, Path.Combine(_dir, "run2"));
        Assert.True(ok, detail);
        Assert.Equal("PLAIN", stdout);
    }

    [Fact]
    public void TheSourceText_IsKeyed()
    {
        string src = Write("src/prog.cob", Plain);
        Assert.False(Through(Options(src)).Hit);
        File.WriteAllText(src, Plain.Replace("\"PLAIN\"", "\"OTHER\""));
        Assert.False(Through(Options(src)).Hit);
    }

    [Fact]
    public void ACopybookEdit_Misses()
    {
        string src = Write("src/prog.cob", WithCopy);
        string cpy = Write("src/PB985CPY.cpy", Copybook("ONE"));
        var o = Options(src, copyPaths: [Path.Combine(_dir, "src")]);
        var first = Through(o);
        Assert.True(first.Result.Success, string.Join("\n", first.Result.Errors));
        Assert.True(Through(o).Hit);
        File.WriteAllText(cpy, Copybook("TWO"));
        var after = Through(o);
        Assert.False(after.Hit, "an edited copybook must miss");
        var (_, stdout, _) = CutRunner.Run(after.Result.OutputDll, Path.Combine(_dir, "run"));
        Assert.Equal("TWO", stdout);
    }

    [Fact]
    public void ACopybookThatNewlyShadowsTheOneFound_Misses()
    {
        // Locating library text is implementor-defined (ISO §7.2.3.4 GR3) and the default library searches its
        // places in order (DOC-A.1-40: the working directory, then each --copy DIR), so a copybook appearing in an
        // EARLIER --copy directory shadows the one the earlier compile found in a later one — an input that exists
        // only as a probe that answered "absent".
        string src = Write("src/prog.cob", WithCopy);
        Write("lib/PB985CPY.cpy", Copybook("LIB"));
        var o = Options(src, copyPaths: [Path.Combine(_dir, "src"), Path.Combine(_dir, "lib")]);
        Assert.True(Through(o).Result.Success);
        Assert.True(Through(o).Hit);
        Write("src/PB985CPY.cpy", Copybook("NEAR"));
        var after = Through(o);
        Assert.False(after.Hit, "a copybook that newly shadows the one found must miss");
        var (_, stdout, _) = CutRunner.Run(after.Result.OutputDll, Path.Combine(_dir, "run"));
        Assert.Equal("NEAR", stdout);
    }

    [Fact]
    public void ADirectivesEnvironmentVariable_IsKeyed()
    {
        const string variable = "PB985-CACHE-PARAM";
        string src = Write("src/prog.cob", """
                   >>DEFINE PB985-CACHE-PARAM AS PARAMETER
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB985ENV.
                   PROCEDURE DIVISION.
                   >>IF PB985-CACHE-PARAM DEFINED
                       DISPLAY "SET"
                   >>ELSE
                       DISPLAY "UNSET"
                   >>END-IF
                       STOP RUN.
            """);
        try
        {
            Environment.SetEnvironmentVariable(variable, null);
            Assert.True(Through(Options(src)).Result.Success);
            Assert.True(Through(Options(src)).Hit);
            Environment.SetEnvironmentVariable(variable, "1");
            var set = Through(Options(src));
            Assert.False(set.Hit, "a directive's environment variable that became set must miss");
            Assert.Equal("SET", CutRunner.Run(set.Result.OutputDll, Path.Combine(_dir, "run")).stdout);
            Assert.True(Through(Options(src)).Hit);
            Environment.SetEnvironmentVariable(variable, "2");
            Assert.False(Through(Options(src)).Hit, "a changed value must miss");
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    [Fact]
    public void AProgramThatReadsTheCompileClock_IsNeverStored()
    {
        string src = Write("src/prog.cob", """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB985CLOCK.
                   PROCEDURE DIVISION.
                       DISPLAY FUNCTION WHEN-COMPILED
                       STOP RUN.
            """);
        var first = Through(Options(src));
        Assert.True(first.Result.Success, string.Join("\n", first.Result.Errors));
        Assert.True(first.Result.Inputs.ReadsCompilationTime);
        Assert.NotNull(CompiledProgramCache.Cacheability(first.Result, Options(src)));
        Assert.False(Through(Options(src)).Hit, "a WHEN-COMPILED program must recompile every time");
        // …and a program that does NOT render WHEN-COMPILED never reads the clock (the lazy capture).
        var plain = CompiledProgramCache.CompileCold(Options(Write("src2/prog.cob", Plain)));
        Assert.False(plain.Inputs.ReadsCompilationTime);
    }

    [Fact]
    public void ARejectedProgram_IsCached_WithItsDiagnostics()
    {
        string src = Write("src/prog.cob", Plain.Replace("DISPLAY WS-A", "DISPLAY WS-NOWHERE"));
        var first = Through(Options(src));
        Assert.False(first.Result.Success);
        var second = Through(Options(src));
        Assert.True(second.Hit);
        Assert.Equal(first.Result.Status, second.Result.Status);
        Assert.Equal(first.Result.Errors, second.Result.Errors);
        Assert.Equal(first.Result.Warnings, second.Result.Warnings);
    }

    /// <summary>Every option changes the key — by REFLECTION over <see cref="CompilerDriver.Options"/>, with the flip
    /// derived from the property's TYPE, so an option added tomorrow is flipped here without an edit (and a property
    /// of a type with no flip fails, naming it). The one exemption is the output DIRECTORY, which must NOT change the
    /// key; <see cref="TheOutputDirectory_DoesNotReachTheOutput"/> proves it may be dropped.</summary>
    [Fact]
    public void EveryOption_ChangesTheKey()
    {
        string src = Write("src/prog.cob", Plain);
        string twin = Write("twin/prog.cob", Plain);   // the same TEXT at another path: the path itself is keyed
        var baseline = Options(src);
        string baseKey = CompiledProgramCache.PrimaryKey(baseline);
        var unflipped = new List<string>();
        foreach (var property in CompiledProgramCache.OptionProperties)
        {
            object? current = property.GetValue(baseline);
            object? flipped = property.Name switch
            {
                nameof(CompilerDriver.Options.SourcePath) => twin,
                nameof(CompilerDriver.Options.OutputPath) => Path.Combine(_dir, "run", "other.dll"),
                _ => Flip(property.PropertyType, current),
            };
            if (flipped is null && current is null) { unflipped.Add($"{property.Name} ({property.PropertyType})"); continue; }
            var options = With(baseline, property, flipped);
            Assert.True(CompiledProgramCache.PrimaryKey(options) != baseKey,
                $"flipping Options.{property.Name} ({current ?? "null"} → {flipped ?? "null"}) did not change the key");
        }
        Assert.True(unflipped.Count == 0, "no flip is derivable for: " + string.Join(", ", unflipped));

        // The deliberate exemption: the output DIRECTORY (same file name) leaves the key unchanged.
        var moved = baseline with { OutputPath = Path.Combine(_dir, "elsewhere", "prog.dll") };
        Assert.Equal(baseKey, CompiledProgramCache.PrimaryKey(moved));
    }

    private static object? Flip(Type type, object? value)
    {
        Type t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(string)) return value is null ? "PB985" : null;
        if (t == typeof(bool)) return !(bool)(value ?? false);
        if (t == typeof(int)) return (int)(value ?? 0) == 2002 ? 2014 : 2002;
        if (t.IsEnum)
            return Enum.GetValues(t).Cast<object>().First(v => !Equals(v, value));
        if (typeof(IEnumerable<string>).IsAssignableFrom(t))
            return value is null ? new List<string> { "pb985-lib" } : null;
        return null;   // no flip for this type: reported by the caller
    }

    private static CompilerDriver.Options With(CompilerDriver.Options o, PropertyInfo property, object? value)
    {
        // A positional record: rebuild through its primary constructor, replacing the one argument.
        var ctor = typeof(CompilerDriver.Options).GetConstructors().Single(c => c.GetParameters().Length > 1);
        object?[] args = [.. ctor.GetParameters().Select(p =>
            string.Equals(p.Name, property.Name, StringComparison.Ordinal) ? value
                : typeof(CompilerDriver.Options).GetProperty(p.Name!)!.GetValue(o))];
        return (CompilerDriver.Options)ctor.Invoke(args);
    }

    /// <summary>The compiler's own bits are keyed: flipping ONE byte of ONE closure assembly changes the
    /// fingerprint, and the closure holds every product assembly and the code generators the compiler runs.</summary>
    [Fact]
    public void TheCompilersOwnBits_AreKeyed()
    {
        var closure = CompiledProgramCache.CompilerClosure();
        var names = closure.Select(a => a.GetName().Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string required in (string[])["Cobol.Net.Compiler", "Cobol.Net.Frontend", "Cobol.Net.Editions",
                     "Cobol.Net.Runtime", "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"])
            Assert.Contains(required, names);
        Assert.Contains(names, n => n.StartsWith("Antlr4.Runtime", StringComparison.Ordinal));
        // Any product assembly loaded into this process after compiling is in the closure (a Cobol.Net.* assembly
        // the compiler reached some other way would run unkeyed).
        CompiledProgramCache.CompileCold(Options(Write("src/prog.cob", Plain)));
        var strays = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name!)
            .Where(n => n.StartsWith("Cobol.Net.", StringComparison.Ordinal) && !n.StartsWith("Cobol.Net.Tests.", StringComparison.Ordinal))
            .Where(n => !names.Contains(n)).ToList();
        Assert.True(strays.Count == 0, "loaded product assemblies outside the keyed closure: " + string.Join(", ", strays));

        string[] copies = [.. closure.Select(a => a.Location).Select(p =>
        {
            string copy = Path.Combine(_dir, "tool", Path.GetFileName(p));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(p, copy);
            return copy;
        })];
        string before = CompiledProgramCache.FingerprintOf(copies);
        byte[] bytes = File.ReadAllBytes(copies[0]);
        bytes[^1] ^= 0x01;
        File.WriteAllBytes(copies[0], bytes);
        Assert.NotEqual(before, CompiledProgramCache.FingerprintOf(copies));
    }

    /// <summary>A DEBUG build's bits do not depend on the commit it was built at (<c>Directory.Build.props</c>): the
    /// SDK's source-control stamp ("1.0.0+&lt;sha&gt;", and the Source Link map whose PDB checksum the assembly
    /// embeds) changed every assembly on every commit, so the lander's re-gate after committing a test-only fix
    /// missed on EVERY case — measured, before the props change. Release keeps the stamp.</summary>
    [Fact]
    public void ADebugBuild_DoesNotStampTheCommit()
    {
        if (TestRepo.Configuration != "Debug") return;
        foreach (var asm in CompiledProgramCache.CompilerClosure().Where(a => a.GetName().Name!.StartsWith("Cobol.Net.", StringComparison.Ordinal)))
        {
            string? version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Assert.False(version?.Contains('+') ?? false,
                $"{asm.GetName().Name} carries a source-revision stamp ({version}) — every commit would change the compiler's bits");
        }
    }

    /// <summary>The output DIRECTORY is the one option not keyed (only the output FILE NAME is): compile the same
    /// programs cold into two directories and every output byte, the diagnostics and the warnings must agree. The
    /// sample is every enabled corpus program that reports a source position at run time (EXCEPTION-LOCATION,
    /// DEBUG-ITEM) — the constructs most likely to carry a path — plus a copybook program and a NIST program.</summary>
    [Fact]
    public void TheOutputDirectory_DoesNotReachTheOutput()
    {
        var sources = new List<CompilerDriver.Options>();
        foreach (object[] row in ConformanceCorpus.EnabledPositive())
        {
            if ((string)row[0] == "shell") continue;
            string path = Path.Combine(ConformanceCorpus.Root, (string)row[0], (string)row[1] + ".cob");
            string text = File.ReadAllText(path);
            if (!Regex.IsMatch(text, "EXCEPTION-LOCATION|DEBUG-ITEM|DEBUG-LINE", RegexOptions.IgnoreCase)) continue;
            sources.Add(ConformanceCorpus.ApplySourceOptions(text,
                new CompilerDriver.Options(path, "x.dll", DialectLevel: int.Parse((string)row[0]), SourceFormat: InitialReferenceFormat.Auto)));
            if (sources.Count == 12) break;
        }
        Assert.NotEmpty(sources);
        Write("cp/PB985CPY.cpy", Copybook("DIR"));
        sources.Add(Options(Write("cp/prog.cob", WithCopy), copyPaths: [Path.Combine(_dir, "cp")]));
        sources.Add(new CompilerDriver.Options(TestRepo.Nist("programs", "NC101A.cob"), "x.dll", NistTestName: "NC101A",
            DialectLevel: 85));

        int i = 0;
        foreach (var o in sources)
        {
            string name = Path.GetFileNameWithoutExtension(o.SourcePath) + ".dll";
            var a = CompiledProgramCache.CompileCold(o with { OutputPath = Path.Combine(_dir, $"a{i}", name) });
            var b = CompiledProgramCache.CompileCold(o with { OutputPath = Path.Combine(_dir, $"b{i}", "deeper", name) });
            i++;
            Assert.Equal(a.Status, b.Status);
            Assert.Equal(a.Errors, b.Errors);
            Assert.Equal(a.Warnings, b.Warnings);
            Assert.Equal(a.OutputFiles.Select(Path.GetFileName), b.OutputFiles.Select(Path.GetFileName));
            foreach (var (fa, fb) in a.OutputFiles.Zip(b.OutputFiles))
                Assert.True(File.ReadAllBytes(fa).AsSpan().SequenceEqual(File.ReadAllBytes(fb)),
                    $"{Path.GetFileName(fa)} of {o.SourcePath} differs between two output directories");
        }
    }

    /// <summary>No compile in this assembly bypasses the cache: the ONE direct <c>CompilerDriver.Compile</c> call is
    /// <see cref="CompiledProgramCache.CompileCold"/>. A new harness that called the driver directly would silently
    /// recompile on every re-gate.</summary>
    [Fact]
    public void EveryCompileInThisAssembly_GoesThroughTheCache()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Tests("Cobol.Net.Tests.Conformance"), "*.cs"))
        {
            if (Path.GetFileName(file) == "CompiledProgramCache.cs") continue;
            string[] lines = File.ReadAllLines(file);
            for (int n = 0; n < lines.Length; n++)
            {
                string code = StripComment(lines[n]);
                if (Regex.IsMatch(code, @"\bCompilerDriver\s*\.\s*Compile\s*\("))
                    offenders.Add($"{Path.GetFileName(file)}:{n + 1}: {lines[n].Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "compile through CompiledProgramCache.Compile (or CompileCold, for a test that must observe a fresh "
            + "compilation) — a direct driver call is recompiled on every re-gate:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>Every AMBIENT read in the compiler goes through <c>CompilationInputs</c>, the record the cache
    /// verifies — the source-form half of "the key is complete". A file, directory, environment, clock or by-name
    /// assembly read anywhere in <c>Cobol.Net.Compiler</c>, <c>Cobol.Net.Frontend</c> or <c>Cobol.Net.Editions</c>
    /// outside <c>CompilationInputs.cs</c> is red unless its line carries its reason — <c>// not a compilation
    /// input: …</c> (the compiler's own deployment, its own output) or <c>// recorded at its call site: …</c>.</summary>
    [Fact]
    public void EveryAmbientReadInTheCompiler_IsRecorded()
    {
        var ambient = new Regex(
            @"\bFile\.(ReadAll\w*|ReadLines|Exists|Open\w*|GetLastWriteTime\w*)\s*\("
            + @"|\bDirectory\.(Exists|Enumerate\w*|Get\w+)\s*\("
            + @"|\bnew\s+(StreamReader|FileStream|FileInfo|DirectoryInfo)\s*\("
            + @"|\bEnvironment\.(GetEnvironmentVariable\w*|CurrentDirectory|MachineName|UserName|TickCount\w*)\b"
            + @"|\bDateTime(Offset)?\.(Now|UtcNow|Today)\b|\bCompileClock\s*\(\s*\)"
            + @"|\bAssembly\.Load\w*\s*\(|\bType\.GetType\s*\(|\bAppContext\.GetData\s*\(");
        var offenders = new List<string>();
        foreach (string project in (string[])["Cobol.Net.Compiler", "Cobol.Net.Frontend", "Cobol.Net.Editions"])
        {
            foreach (string file in Directory.EnumerateFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(TestRepo.Src(), file);
                if (Regex.IsMatch(rel, @"[\\/](bin|obj|Generated)[\\/]")) continue;
                if (Path.GetFileName(file) == "CompilationInputs.cs") continue;
                string[] lines = File.ReadAllLines(file);
                for (int n = 0; n < lines.Length; n++)
                {
                    if (lines[n].Contains("// not a compilation input:", StringComparison.Ordinal)
                        || lines[n].Contains("// recorded at its call site:", StringComparison.Ordinal)) continue;
                    if (ambient.IsMatch(StripComment(lines[n]))) offenders.Add($"{rel}:{n + 1}: {lines[n].Trim()}");
                }
            }
        }
        Assert.True(offenders.Count == 0,
            "an ambient read the compiled-program cache (and any incremental build) cannot see — route it through "
            + "CompilationInputs, or say on the line why it is not a compilation input:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>No PUBLIC mutable static in the compiler: a process-global a test could set before compiling would be
    /// an input no key can see. (The one internal seam, <c>IntrinsicBinder.CompileClock</c>, is unreachable from this
    /// assembly and its every read is recorded.)</summary>
    [Fact]
    public void TheCompiler_HasNoPublicMutableStatic()
    {
        var offenders = new List<string>();
        foreach (var asm in CompiledProgramCache.CompilerClosure()
                     .Where(a => a.GetName().Name is "Cobol.Net.Compiler" or "Cobol.Net.Frontend" or "Cobol.Net.Editions"))
        {
            // The ANTLR tool's generated recognizers expose display-name tables (channelNames, modeNames) as
            // public static arrays; they are build output the compiler never assigns, not a settable input.
            foreach (var type in asm.GetExportedTypes().Where(t => t.Namespace != "CobolNet.Frontend.Generated"))
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
                offenders.AddRange(type.GetFields(flags).Where(f => !f.IsInitOnly && !f.IsLiteral)
                    .Select(f => $"{type.FullName}.{f.Name}"));
                offenders.AddRange(type.GetProperties(flags).Where(p => p.SetMethod is { IsPublic: true })
                    .Select(p => $"{type.FullName}.{p.Name}"));
            }
        }
        Assert.True(offenders.Count == 0, "public mutable statics (unkeyed process-global compiler inputs): "
            + string.Join(", ", offenders));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "true", false)]      // CI runs cold unless it opts in
    [InlineData("on", "true", true)]
    [InlineData("off", null, false)]
    [InlineData("OFF", null, false)]
    [InlineData("0", null, false)]
    public void TheSwitch_DecidesAsDocumented(string? switchValue, string? ci, bool enabled) =>
        Assert.Equal(enabled, CompiledProgramCache.ResolveEnabled(switchValue, ci));

    private static string StripComment(string line)
    {
        int at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }
}
