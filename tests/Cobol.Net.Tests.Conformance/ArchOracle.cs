// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE ARCHITECTURE REVIEW'S BEHAVIOR-NEUTRALITY ORACLE (kb/Work PB2116; docs/rearchitecture/
/// DESIGN-architecture-review.md §4 items 1 and 2). A refactor can change what the compiler emits through
/// registration order, static initialization or a shared cache while every test stays green; this records, for
/// every compile this assembly's partitioned theory families perform (the conformance corpus, NIST and the version
/// matrix), the emitted C# and the diagnostic stream, so a restructuring wave can prove case by case that it changed
/// neither.
/// <para><b>The population is the suites' own.</b> Every case is enumerated from a <see cref="RowSource"/> — the
/// <c>[PartitionedRowSource]</c> member a partitioned theory family consumes — and compiled through the SAME option
/// builders and harness paths the theories call (<see cref="ConformanceCorpus.PositiveOptions"/>,
/// <see cref="ConformanceCorpus.NegativeCase"/>, <see cref="CorpusManifest.CompileOptions(string, string)"/>,
/// <see cref="EditionHarness.CompileStaged{T}"/>, <see cref="EditionHarness.CompileNistObserved{T}"/>). A golden
/// added to a manifest is therefore captured with no edit here, and <c>ArchOracleDriftTests</c> holds every row
/// source of this assembly enrolled, so a new population cannot escape either.</para>
/// <para><b>An observation is portable.</b> The repository root and the per-compile scratch directory are written
/// as <c>&lt;repo&gt;</c> and <c>&lt;scratch&gt;</c> with forward slashes, line endings are LF, and the
/// WHEN-COMPILED stamp of a compilation that reads the compile clock is masked, so the same commit captures the
/// same hashes on Windows and on Linux.</para>
/// <para>The capture runs OUTSIDE xunit, through <c>tests/Cobol.Net.ArchOracle</c> (driven by
/// <c>scripts/arch/capture_oracle.py</c>), with the compiled-program cache switched off: the oracle observes the
/// compiler, never a stored result.</para>
/// </summary>
public static class ArchOracle
{
    /// <summary>What one case observed: the emitted C# (null when the backend never ran) and the canonical
    /// diagnostic stream — outcome, compile-time output, errors and warnings, in the order the driver produced them
    /// (the order is observed, not sorted: a refactor that reorders two diagnostics is a difference to explain).</summary>
    internal sealed record Observation(string? CSharp, string Diagnostics);

    /// <summary>One compile configuration the suites perform, by its stable identity.</summary>
    /// <param name="Id">The case identity, e.g. <c>corpus/2023/pb803_sign_encoding_ascii</c>,
    /// <c>matrix/goback-raising@2014+permissive</c>, <c>negative/pb1234_x</c>.</param>
    /// <param name="Population">The suite and edition it is counted under, e.g. <c>corpus/2023</c>.</param>
    /// <param name="Observe">Compile the case and observe it.</param>
    internal sealed record OracleCase(string Id, string Population, Func<Observation> Observe);

    /// <summary>One enrolled row source: a partitioned theory family's unpartitioned row member, and how one of its
    /// rows becomes oracle cases (none for the family's empty-manifest sentinel row).</summary>
    /// <param name="Family">The generic theory family declaring the row member.</param>
    /// <param name="Member">The <c>[PartitionedRowSource]</c> member's name.</param>
    /// <param name="Rows">The member itself.</param>
    /// <param name="ToCases">Row → the cases the family's theory compiles for it.</param>
    internal sealed record RowSource(
        Type Family, string Member, Func<IEnumerable<object[]>> Rows, Func<object[], IEnumerable<OracleCase>> ToCases);

    /// <summary>Every enrolled row source — exactly the assembly's <c>[PartitionedRowSource]</c> members
    /// (<c>ArchOracleDriftTests</c>) — each with the compile its theory performs for a row.</summary>
    internal static IReadOnlyList<RowSource> Sources { get; } =
    [
        new(typeof(CorpusRunnerTestsBase<>), nameof(CorpusRunnerTestsBase<Slot0>.AllEnabledPositive),
            CorpusRunnerTestsBase<Slot0>.AllEnabledPositive, CorpusPositive),
        new(typeof(CorpusRunnerTestsBase<>), nameof(CorpusRunnerTestsBase<Slot0>.AllEnabledNegative),
            CorpusRunnerTestsBase<Slot0>.AllEnabledNegative, CorpusNegative),
        new(typeof(NistDifferentialTestsBase<>), nameof(NistDifferentialTestsBase<Slot0>.AllGreenPrograms),
            NistDifferentialTestsBase<Slot0>.AllGreenPrograms, NistGolden),
        new(typeof(VersionMatrixTestsBase<>), nameof(VersionMatrixTestsBase<Slot0>.AllMatrix),
            VersionMatrixTestsBase<Slot0>.AllMatrix, row => MatrixCell(row, permissive: false)),
        new(typeof(VersionMatrixTestsBase<>), nameof(VersionMatrixTestsBase<Slot0>.AllIntroducedMatrix),
            VersionMatrixTestsBase<Slot0>.AllIntroducedMatrix, row => MatrixCell(row, permissive: true)),
        new(typeof(VersionMatrixTestsBase<>), nameof(VersionMatrixTestsBase<Slot0>.AllRemovedMatrix),
            VersionMatrixTestsBase<Slot0>.AllRemovedMatrix, row => MatrixCell(row, permissive: true)),
        // The obsolete theory compiles strict at its row's edition — the very compile the strict matrix cell is, so
        // its cases coincide with AllMatrix's by identity and are counted once.
        new(typeof(VersionMatrixTestsBase<>), nameof(VersionMatrixTestsBase<Slot0>.AllObsoleteMatrix),
            VersionMatrixTestsBase<Slot0>.AllObsoleteMatrix, row => MatrixCell(row, permissive: false)),
        new(typeof(VersionMatrixTestsBase<>), nameof(VersionMatrixTestsBase<Slot0>.AllContinuityCells),
            VersionMatrixTestsBase<Slot0>.AllContinuityCells, ContinuityCell),
    ];

    /// <summary>The whole population: every enrolled source's cases, one per identity, in ordinal id order. An id
    /// names its compile completely (suite, program or construct, edition, severity axis), so two sources yielding
    /// one id — the strict matrix and obsolete cells, a chain predecessor that is also a green row — yield one
    /// compile, and it is captured once.</summary>
    internal static IReadOnlyList<OracleCase> Population()
    {
        var byId = new Dictionary<string, OracleCase>(StringComparer.Ordinal);
        foreach (var source in Sources)
        {
            foreach (object[] row in source.Rows())
            {
                foreach (var c in source.ToCases(row))
                {
                    byId.TryAdd(c.Id, c);
                }
            }
        }

        return [.. byId.Values.OrderBy(c => c.Id, StringComparer.Ordinal)];
    }

    // ── the row → case mappings: each compiles exactly what its theory compiles ─────────────────────────────────

    private static IEnumerable<OracleCase> CorpusPositive(object[] row)
    {
        var (edition, name) = ((string)row[0], (string)row[1]);
        if (edition == "shell") yield break;   // ConformanceCorpus.EnabledPositive's empty-manifest sentinel
        yield return new($"corpus/{edition}/{name}", $"corpus/{edition}", () => InScratch(dir =>
            CompiledProgramCache.Compile(ConformanceCorpus.PositiveOptions(edition, name, Path.Combine(dir, name + ".dll")))));
    }

    private static IEnumerable<OracleCase> CorpusNegative(object[] row)
    {
        string name = (string)row[0];
        if (name == "sentinel") yield break;   // ConformanceCorpus.EnabledNegative's empty-manifest sentinel
        yield return new($"negative/{name}", "negative", () =>
        {
            var (source, editions) = ConformanceCorpus.NegativeCase(name);
            return Fold(editions.Select(ed => ($"@{ed}", EditionHarness.CompileStaged(
                source, ed, permissive: false, copybooks: null, flagExtensions: false, Observe))));
        });
    }

    /// <summary>A green∪divergent NIST program at the golden run's edition, and the chain predecessors its golden run
    /// compiles first (a predecessor is its own case, compiled by the same options).</summary>
    private static IEnumerable<OracleCase> NistGolden(object[] row)
    {
        string name = (string)row[0];
        IEnumerable<string> programs = CorpusManifest.Chains.TryGetValue(name, out var preds) ? [.. preds, name] : [name];
        foreach (string p in programs)
        {
            yield return new($"nist/{p}", $"nist/{CorpusManifest.GoldenRunEdition}", () => InScratch(dir =>
                CompiledProgramCache.Compile(CorpusManifest.CompileOptions(p, Path.Combine(dir, p + ".dll")))));
        }
    }

    /// <summary>An INV-1 continuity cell: the program checked permissive, then strict, at the row's edition.</summary>
    private static IEnumerable<OracleCase> ContinuityCell(object[] row)
    {
        var (name, edition) = ((string)row[0], (int)row[1]);
        yield return new($"nist-continuity/{name}@{edition}", $"nist-continuity/{edition}", () => Fold(
            from permissive in new[] { true, false }
            select (permissive ? "permissive" : "strict",
                EditionHarness.CompileNistObserved(name, edition, permissive, checkOnly: true, Observe))));
    }

    private static IEnumerable<OracleCase> MatrixCell(object[] row, bool permissive)
    {
        var (id, edition) = ((string)row[0], (int)row[1]);
        var c = VersionMatrixCatalogue.ById[id];
        yield return new($"matrix/{id}@{edition}{(permissive ? "+permissive" : "")}",
            $"matrix/{edition}{(permissive ? "+permissive" : "")}",
            () => EditionHarness.CompileStaged(c.Source, edition, permissive, c.Copybooks, flagExtensions: false, Observe));
    }

    // ── observation ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Compile in a fresh scratch directory (the corpus runner's and the golden run's shape: the source stays
    /// where it is, the outputs go to scratch) and observe it before the directory is removed.</summary>
    private static Observation InScratch(Func<string, CompilerDriver.Result> compile)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Oracle_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            return Observe(dir, compile(dir));
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Several compiles of one case (a negative fixture's reject-at editions; a continuity cell's two
    /// axes), each section headed by its label.</summary>
    private static Observation Fold(IEnumerable<(string Label, Observation Observation)> parts)
    {
        var diagnostics = new StringBuilder();
        var csharp = new StringBuilder();
        bool anyCSharp = false;
        foreach (var (label, o) in parts)
        {
            diagnostics.Append("== ").Append(label).Append('\n').Append(o.Diagnostics);
            if (o.CSharp is null) continue;
            anyCSharp = true;
            csharp.Append("== ").Append(label).Append('\n').Append(o.CSharp);
        }

        return new(anyCSharp ? csharp.ToString() : null, diagnostics.ToString());
    }

    /// <summary>The canonical observation of one driver result whose outputs went to <paramref name="scratch"/>.</summary>
    internal static Observation Observe(string scratch, CompilerDriver.Result r)
    {
        var diagnostics = new StringBuilder();
        diagnostics.Append("outcome=").Append(r.Status).Append('\n');
        foreach (var line in r.CompileOutput)
            diagnostics.Append("output[").Append(line.Stream).Append("]: ").Append(line.Text).Append('\n');
        foreach (string e in r.Errors)
            diagnostics.Append("error: ").Append(e).Append('\n');
        foreach (string w in r.Warnings)
            diagnostics.Append("warning: ").Append(w).Append('\n');
        if (r.Inputs.ReadsCompilationTime)
            diagnostics.Append("reads-compilation-time\n");

        // A driver that names a .g.cs it did not write is a capture failure, never "no C#": the read throws.
        string? csharp = r.GeneratedCsPath is { } path ? File.ReadAllText(path) : null;
        // ISO §15.99.3 2): WHEN-COMPILED's "returned value is the date and time of compilation" — the one output a
        // correct compiler changes run to run. Masked only in a compilation that recorded reading the clock (kb/Work PB985's input record).
        if (csharp is not null && r.Inputs.ReadsCompilationTime)
            csharp = WhenCompiledStamp.Replace(csharp, "<when-compiled>");
        return new(csharp is null ? null : Portable(csharp, scratch), Portable(diagnostics.ToString(), scratch));
    }

    /// <summary>One path segment as it appears in a diagnostic or a C# literal: anything up to a separator,
    /// whitespace, a quote, or the punctuation that ends a path there (<c>prog.cob(11,1):</c>).</summary>
    private const string PathSegment = @"[^\s""'(),;:<>|\\/]*";

    /// <summary>The 21-character WHEN-COMPILED layout (YYYYMMDDhhmmsshh followed by the signed UTC offset).</summary>
    private static readonly Regex WhenCompiledStamp = new(@"\d{16}[+-]\d{4}", RegexOptions.CultureInvariant);

    /// <summary>LF line endings, and the scratch directory and the repository root written as <c>&lt;scratch&gt;</c>
    /// and <c>&lt;repo&gt;</c> with forward-slash separators — in each spelling a path takes in a diagnostic or in
    /// emitted C# (native separators, a C# string literal's doubled backslashes, forward slashes).</summary>
    internal static string Portable(string text, string scratch)
    {
        text = text.Replace("\r\n", "\n");
        foreach (var (root, token) in new[] { (scratch, "<scratch>"), (TestRepo.Root, "<repo>") })
        {
            string trimmed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            foreach (string separator in new[] { @"\\", @"\", "/" })
            {
                // The root itself (a default-library search names the bare working directory) or the root and the
                // segments under it; never a prefix of a longer sibling name.
                string spelled = trimmed.Replace(@"\", separator).Replace("/", separator);
                var pattern = new Regex(
                    Regex.Escape(spelled) + "(?<rest>(?:" + Regex.Escape(separator) + PathSegment + ")*)(?!" + PathSegment[..^1] + ")",
                    RegexOptions.CultureInvariant);
                text = pattern.Replace(text, m => token + m.Groups["rest"].Value.Replace(separator, "/"));
            }
        }

        return text;
    }

    // ── the capture entry (tests/Cobol.Net.ArchOracle) ──────────────────────────────────────────────────────────

    /// <summary>
    /// <c>capture --out DIR [--jobs N]</c> compiles every case and writes, under DIR, each case's emitted C#
    /// (<c>&lt;id&gt;.g.cs</c>) and diagnostic stream (<c>&lt;id&gt;.diag.txt</c>), and <c>cases.tsv</c> — one line
    /// per case, ordinal order: id, population, SHA-256 of the C# (<c>-</c> when none), SHA-256 of the diagnostics.
    /// <c>list</c> prints the population (id TAB population) without compiling. Exit 0 on success; 2 on a usage
    /// error; 3 when the compiled-program cache is on (the oracle must observe the compiler); 4 when a case threw.
    /// </summary>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        string? verb = args.Length > 0 ? args[0] : null;
        string? outDir = null;
        int jobs = Environment.ProcessorCount;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length: outDir = args[++i]; break;
                case "--jobs" when i + 1 < args.Length && int.TryParse(args[i + 1], out int j) && j > 0: jobs = j; i++; break;
                default: return Usage(stderr, $"unrecognized argument '{args[i]}'");
            }
        }

        if (verb == "list")
        {
            foreach (var c in Population())
                stdout.WriteLine($"{c.Id}\t{c.Population}");
            return 0;
        }

        if (verb != "capture" || outDir is null) return Usage(stderr, "expected 'list' or 'capture --out DIR'");
        if (CompiledProgramCache.Enabled)
        {
            stderr.WriteLine($"error: the compiled-program cache is on; run with {CompiledProgramCache.SwitchVariable}=off");
            return 3;
        }

        var cases = Population();
        var lines = new string[cases.Count];
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.For(0, cases.Count, new ParallelOptions { MaxDegreeOfParallelism = jobs }, i =>
        {
            var c = cases[i];
            Observation o;
            try { o = c.Observe(); }
            catch (Exception ex) { failures.Add($"{c.Id}: {ex.GetType().Name}: {ex.Message}"); return; }
            string stem = Path.Combine(outDir, c.Id.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(stem)!);
            File.WriteAllText(stem + ".diag.txt", o.Diagnostics);
            if (o.CSharp is not null) File.WriteAllText(stem + ".g.cs", o.CSharp);
            lines[i] = $"{c.Id}\t{c.Population}\t{(o.CSharp is null ? "-" : Sha256(o.CSharp))}\t{Sha256(o.Diagnostics)}";
        });

        if (!failures.IsEmpty)
        {
            foreach (string f in failures.Order(StringComparer.Ordinal))
                stderr.WriteLine($"error: {f}");
            return 4;
        }

        File.WriteAllText(Path.Combine(outDir, "cases.tsv"), string.Join("\n", lines) + "\n");
        stdout.WriteLine($"captured {cases.Count} cases into {outDir}");
        return 0;
    }

    private static int Usage(TextWriter stderr, string message)
    {
        stderr.WriteLine($"error: {message}");
        stderr.WriteLine("usage: Cobol.Net.ArchOracle list | capture --out DIR [--jobs N]");
        return 2;
    }

    /// <summary>Lower-case hex SHA-256 of the UTF-8 text.</summary>
    internal static string Sha256(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
