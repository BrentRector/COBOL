// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Drives real NIST CCVS programs through WiseOwl COBOL end-to-end and compares the produced output to the NIST golden
/// (<c>tests/nist/valid/&lt;TEST&gt;.txt</c>) on the guard's acceptance basis (drop CR, strip per-line trailing spaces,
/// and mask the volatile COMPUTED= operand). The golden is the authoritative oracle — it was validated against the
/// legacy byte engine over the whole 364-program corpus — so a match here proves WiseOwl COBOL runs the program correctly,
/// not merely that it agrees with the legacy. This is the harness the G5 corpus drive runs through: each NC/SM/IC/…
/// program that goes green becomes a permanent regression test by adding its name here.
/// </summary>
/// <remarks>
/// ⛔ THIS CLASS IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). On the battery #41 trx it was
/// the Conformance leg's SECOND pole: 349 theory rows running SERIALLY for 237.9 s, because xUnit 2.9.2 makes each
/// test CLASS one collection. The rows are declared ONCE here and sliced <c>index % <see cref="Partitions"/></c>
/// ways by the concrete <c>NistDifferentialTests_P0 … _P5</c> below. The partitions keep
/// <c>NistDifferentialTests</c> in their names so every existing <c>FullyQualifiedName~NistDifferentialTests</c>
/// filter (CI's shard matrix, plan §0) still selects them unchanged.
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class NistDifferentialTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost: 237.9 s ÷ 6 ≈ 40 s per collection, which
    /// puts this class below the assembly's next-largest (the split version matrix, ~60 s).</summary>
    public const int Partitions = 6;

    // The green∪divergent NIST set now lives in tests/nist/corpus.tsv — the ONE source of truth (folds the former
    // per-program [InlineData] list and chains.tsv; loaded by CorpusManifest). Adding a
    // green program is a manifest row, not a code edit. Per-program provenance (subsystem + ISO §) is in DEVLOG + git
    // history; a divergent row carries its ISO § citation in the corpus.tsv note.
    [PartitionedRowSource(nameof(GreenPrograms))]
    public static IEnumerable<object[]> AllGreenPrograms() => CorpusManifest.GreenData();

    /// <summary>This partition's share of the green∪divergent NIST corpus.</summary>
    public static IEnumerable<object[]> GreenPrograms() =>
        TestPartitioning.SliceRows<TSlot>(AllGreenPrograms(), Partitions);

    [Theory]
    [MemberData(nameof(GreenPrograms))]
    public void NistProgram_MatchesGolden(string testName)
    {
        string goldenPath = TestRepo.Nist("valid", testName + ".txt");
        Assert.True(File.Exists(goldenPath), $"golden not found: {goldenPath}");

        // A row that declares TERMINATES asserts the abnormal termination itself, not the golden: the golden records
        // a run that continued past a fatal I-O status nothing covers, which Annex A.1 item 103 now ends.
        if (CorpusManifest.Rows.First(r => string.Equals(r.Name, testName, StringComparison.OrdinalIgnoreCase)).ExpectedTermination is { } ec)
        {
            var (ranClean, _, terminated) = RunNist(testName);
            Assert.False(ranClean, $"{testName} is declared to terminate on {ec} but ran clean");
            Assert.Contains("exit non-zero", terminated);
            Assert.Contains(ec, terminated);
            return;
        }

        var (ok, output, detail) = RunNist(testName);
        Assert.True(ok, detail);
        Assert.Equal(Normalize(File.ReadAllText(goldenPath)), output);
    }

    // Producer→consumer chains come from CorpusManifest.Chains (folded into tests/nist/corpus.tsv from the former
    // chains.tsv). A chain consumer's first op on the shared TF### file is OPEN INPUT/I-O of a file its predecessors
    // create, so RunNist compiles and runs the predecessors (in order) inside the consumer's OWN isolated directory
    // first — deterministic start-clean, zero cross-test coupling under xunit parallelism.

    /// <summary>The per-edition override for the INV-1-STRONG behavioral leg (the roadmap's fatal-challenge fix,
    /// attached to the P2.7 flip; promoted to a G7 exit criterion at Phase 8): <c>COBOLNET_NIST_STD</c>
    /// (85|2002|2014|2023, default 85) + <c>COBOLNET_NIST_PERMISSIVE=1</c> re-target the WHOLE golden run — e.g.
    /// <c>COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1</c> compiles AND RUNS all 318 goldens at the
    /// shipping default edition in migration mode, asserting byte-identical output.</summary>
    private static readonly int NistStd =
        int.TryParse(Environment.GetEnvironmentVariable("COBOLNET_NIST_STD"), out int v) ? v : 85;
    private static readonly bool NistPermissive =
        Environment.GetEnvironmentVariable("COBOLNET_NIST_PERMISSIVE") == "1";

    /// <summary>Compile a NIST program (with CCVS X-card preprocessing) and run it in an isolated temp directory —
    /// chain predecessors first, when <c>chains.tsv</c> lists any — returning the program's output read from its
    /// print file (the CCVS report) — or stdout for a DISPLAY-only program — normalized to the NIST acceptance
    /// basis.</summary>
    private static (bool ok, string output, string detail) RunNist(string testName)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Nist_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = TestRepo.Nist("programs", testName + ".cob");
            if (!File.Exists(src)) return (false, "", $"source not found: {src}");
            string dll = Path.Combine(dir, testName + ".dll");

            // Guard parity (scripts/guard.sh:120): the CCVS-85 switch programs (NC174A/NC254A) run with external
            // SWITCH-1 ON, SWITCH-2 unset — their goldens assume exactly that.
            var env = new Dictionary<string, string> { ["COBOL_SWITCH_1"] = "ON" };

            // Chain predecessors: each producer compiles from its OWN .cob and runs in THIS directory so the
            // consumer's shared TF### input files exist. Chains are self-sufficient in an isolated dir — every
            // chain's first member re-creates its file via OPEN OUTPUT — so the legacy guard's cross-chain
            // ordering constraints do not carry over.
            bool chained = CorpusManifest.Chains.TryGetValue(testName, out var predecessors);
            if (chained)
                foreach (string p in predecessors!)
                {
                    string pSrc = TestRepo.Nist("programs", p + ".cob");
                    string pDll = Path.Combine(dir, p + ".dll");
                    var pResult = CompiledProgramCache.Compile(new CompilerDriver.Options(pSrc, pDll, NistTestName: p,
                        DialectLevel: NistStd, Permissive: NistPermissive));
                    if (!pResult.Success)
                        return (false, "", $"[chain {p}] compile {pResult.Status}: {string.Join("\n", pResult.Errors)}");
                    string pDat = TestRepo.Nist("data", p + ".dat");
                    var (pOk, _, pDetail) = CutRunner.Run(pDll, dir, File.Exists(pDat) ? pDat : null, env);
                    if (!pOk) return (false, "", $"[chain {p}] run exit non-zero: {pDetail}");
                }

            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, NistTestName: testName,
                DialectLevel: NistStd, Permissive: NistPermissive));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            string dat = TestRepo.Nist("data", testName + ".dat");
            var (runOk, stdout, runDetail) = CutRunner.Run(dll, dir, File.Exists(dat) ? dat : null, env);
            if (!runOk) return (false, "", $"[run] exit non-zero: {runDetail}");

            // The CCVS report lands in the print file (assign target → <lowercased>.txt in the run dir); a
            // DISPLAY-only program produces no print file and is read from stdout — exactly the guard's discovery
            // order. In a chain directory the any-*.txt fallback would pick a predecessor's report or a tf###
            // data file, so it is disabled there (the consumer's print file is found by exact name only).
            string printFile = Path.Combine(dir, testName.ToLowerInvariant() + ".txt");
            string raw = File.Exists(printFile) ? File.ReadAllText(printFile)
                : !chained && Directory.EnumerateFiles(dir, "*.txt").FirstOrDefault() is { } any ? File.ReadAllText(any)
                : stdout;
            return (true, Normalize(raw), runDetail);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The NIST acceptance basis (exactly <c>scripts/guard.sh</c>'s <c>normalize()</c>): drop CR, strip
    /// per-line trailing spaces, and mask the COMPUTED= operand (a value some CCVS programs print that is not part of
    /// the pass/fail decision). Applied identically to the golden and the produced output.
    /// <para>⛔ A LONE CR IS DROPPED, NOT TURNED INTO A LINE BREAK. <c>ReplaceLineEndings("\n")</c> — what this used
    /// to call — maps a bare CR to LF, which silently ignored the basis above: a WRITE … ADVANCING 0 is an OVERPRINT
    /// written as a carriage return (docs/CONFORMANCE.md "ADVANCING 0"; kb/Work PB1667), and the NIST print programs
    /// (SQ101M, SQ207M) check that the overprinted line follows its marker on the same line. The basis exists to
    /// compare listing TEXT, and a carriage return is not text.</para></summary>
    private static string Normalize(string s)
    {
        var lines = s.Replace("\r\n", "\n").Replace("\r", "").ReplaceLineEndings("\n").Split('\n')
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(' '), "COMPUTED=  [0-9]*", "COMPUTED=  XXXXXXXXX"));
        return string.Join("\n", lines).TrimEnd('\n');
    }
}

// ⛔ THE SIX PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover the corpus exactly
// once; adding or removing one without changing NistDifferentialTestsBase<>.Partitions is a RED, not a silent
// drop of NIST programs from the regression net.

/// <summary>NIST golden partition 0 of <see cref="NistDifferentialTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class NistDifferentialTests_P0 : NistDifferentialTestsBase<Slot0>;

/// <summary>NIST golden partition 1.</summary>
public sealed class NistDifferentialTests_P1 : NistDifferentialTestsBase<Slot1>;

/// <summary>NIST golden partition 2.</summary>
public sealed class NistDifferentialTests_P2 : NistDifferentialTestsBase<Slot2>;

/// <summary>NIST golden partition 3.</summary>
public sealed class NistDifferentialTests_P3 : NistDifferentialTestsBase<Slot3>;

/// <summary>NIST golden partition 4.</summary>
public sealed class NistDifferentialTests_P4 : NistDifferentialTestsBase<Slot4>;

/// <summary>NIST golden partition 5.</summary>
public sealed class NistDifferentialTests_P5 : NistDifferentialTestsBase<Slot5>;
