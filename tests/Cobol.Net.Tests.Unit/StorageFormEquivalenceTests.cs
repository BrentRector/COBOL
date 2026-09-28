// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The StorageForm equivalence HARNESS — the bind-and-verify half, held in ONE non-generic type so the crafted
/// cases and the eight NIST-corpus partitions share it rather than duplicating it.
/// </summary>
internal static class StorageFormEquivalence
{
    /// <summary>Bind <paramref name="source"/> via the full pipeline and assert zero StorageForm divergences.</summary>
    internal static void VerifySource(string source, int dialect)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_sf_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, source);
        try
        {
            var (divergences, ok) = Analyze(path, dialect);
            Assert.True(ok, $"frontend failed to parse the crafted source");
            Assert.True(divergences.Count == 0,
                $"{divergences.Count} StorageForm divergence(s):\n" + string.Join("\n", divergences.Take(10)));
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>Bind one .cob file and return (divergences, parsed-ok). A program that fails the FRONTEND (parse /
    /// missing COPY) is skipped (parsed-ok=false) — it exercises no StorageForm.</summary>
    /// <param name="nistTestName">A NIST CCVS program is read as the NIST harness reads it (<c>--nist</c>): its
    /// column-7 letters are CCVS conventions only in that dialect (kb/Work PB1494), never §6.2.2 indicators.</param>
    internal static (List<string> Divergences, bool Ok) Analyze(string path, int dialect, string? nistTestName = null)
    {
        var diags = new DiagnosticBag();
        var frontend = new CnFrontend
        {
            InitialFormat = InitialReferenceFormat.Auto, DialectLevel = dialect, NistTestName = nistTestName,
        };
        // Resolve the NIST COPY library the SM/COPY suite needs (sibling copylib/, the CLI convention).
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } srcDir
            && Path.GetFullPath(Path.Combine(srcDir, "..", "copylib")) is { } copylib && Directory.Exists(copylib))
            frontend.AddCopySearchPath(copylib);
        var tree = frontend.Parse(path, diags);
        if (tree is null || diags.HasErrors) return ([], false);

        var emitter = new CSharpEmitter();
        var edition = new EditionContext(dialect, permissive: false);
        var bound = emitter.Bind(tree, edition, frontend.Directives);   // runs MarkStoreAsImage + OO harmonize + StorageFormPass.Compute

        var binders = bound.Units.Select(u => u.Data)
            .Concat(bound.ClassUnits.SelectMany(c => new[] { c.Data, c.FactoryData }));
        var divergences = new List<string>();
        foreach (var b in binders)
        {
            divergences.AddRange(StorageFormPass.Verify(b));
        }
        return (divergences, true);
    }

    /// <summary>The NIST corpus as theory rows — one row per program path, ordered so the partition stride is
    /// stable across runs.</summary>
    internal static IEnumerable<object[]> NistPrograms()
    {
        string dir = TestRepo.Nist("programs");
        Assert.True(Directory.Exists(dir), $"NIST corpus dir not found: {dir}");
        return Directory.GetFiles(dir, "*.cob").OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => new object[] { p });
    }

    internal const string WholeGroupMove = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. WGM.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 A PIC 9(4).
           05 B PIC X(3).
        01 DEST PIC X(7).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    internal const string FixedOccursUnderWholeGroup = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FOW.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 T PIC 9(2) OCCURS 3 TIMES.
        01 DEST PIC X(6).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    internal const string SignSeparate = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SGS.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 S PIC S9(4) SIGN LEADING SEPARATE.
           05 F PIC X(2).
        01 DEST PIC X(7).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    internal const string MixedNativeUsages = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. MNU.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS.
           05 N1 PIC 9(4).
           05 N2 PIC 9(6) COMP.
           05 N3 PIC 9(8) COMP-3.
           05 N4 COMP-1.
           05 N5 PIC S9(18).
           05 A1 PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY N1 N2 N3 N4 N5 A1.
            STOP RUN.
        """;

    internal const string RedefinesView = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RDV.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 REC.
           05 PART-A PIC X(6).
           05 PART-B REDEFINES PART-A.
              10 P1 PIC 9(3).
              10 P2 PIC X(3).
        PROCEDURE DIVISION.
        MAIN.
            MOVE "ABCDEF" TO PART-A.
            DISPLAY P1.
            STOP RUN.
        """;
}

/// <summary>
/// The PHASE-05 Step 2 (D0) prove-then-delete gate: the new <see cref="StorageFormPass"/> computes a canonical
/// <see cref="Model.StorageForm"/> in parallel with the legacy <c>StoreAsImage</c> flag + the recursive image-fact
/// properties, and this test asserts they are EQUAL corpus-wide (four identities per item — see
/// <see cref="StorageFormPass.Verify"/>) BEFORE any deletion. Drives the FULL <c>emitter.Bind</c> path (which runs
/// <c>MarkStoreAsImage</c> + the OO harmonize + <c>StorageFormPass.Compute</c>) — NOT the bare <c>DataBinder.Bind</c>,
/// which skips the middle-end passes. (Exit criterion #3.)
/// <para>This class holds the CRAFTED cases only; the corpus-wide sweep is the partitioned
/// <see cref="StorageFormNistEquivalenceTestsBase{TSlot}"/> below.</para>
/// </summary>
public sealed class StorageFormEquivalenceTests
{
    [Theory]
    // A whole-group MOVE promotes the group's numeric-DISPLAY leaf to its string image (§14.9.25.4 GR4) — Storage must be
    // CharImage(Numeric) (ElementType "string"), exactly as the legacy StoreAsImage flag makes it.
    [InlineData(StorageFormEquivalence.WholeGroupMove)]
    // A fixed-OCCURS numeric-DISPLAY element under a whole group is promoted per occurrence (element type string[]).
    [InlineData(StorageFormEquivalence.FixedOccursUnderWholeGroup)]
    // SIGN IS SEPARATE adds a character position — the amended NativeInt.Width (digits + 1) must reproduce ImageWidth.
    [InlineData(StorageFormEquivalence.SignSeparate)]
    // Mixed native usages NOT under a whole group stay native (NativeInt/NativeFloat), a group summing their widths.
    [InlineData(StorageFormEquivalence.MixedNativeUsages)]
    // A REDEFINES view (Tier classification → TierBWindow / CharImage) — parity with StoreAsImage on the members.
    [InlineData(StorageFormEquivalence.RedefinesView)]
    public void CraftedCases(string source) => StorageFormEquivalence.VerifySource(source, 2002);
}

/// <summary>
/// The corpus-wide half of the StorageForm prove-then-delete gate: every NIST program binds through the full
/// pipeline and must show zero StorageForm↔StoreAsImage divergences.
/// </summary>
/// <remarks>
/// ⛔ THIS SWEEP IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). As ONE test it took
/// <b>171.5 s</b> on the battery #41 trx and <i>was</i> the Unit assembly's entire 171 s wall clock while 31 cores
/// idled (the leg's average concurrency was 1.9x). Splitting it eight ways puts each collection at ≈21 s, below
/// the leg's next-largest class (<c>GrammarDiagramGeneratorDriftTests</c>, 35.7 s, a single indivisible test).
/// <para>⚠ THE POPULATION ASSERTION SURVIVES THE SPLIT, and that is deliberate — a MISSING observation is not a
/// negative one. The whole-corpus form asserted <c>parsed &gt;= 50</c>, i.e. that the sweep actually bound
/// programs rather than skipping all of them; each partition now asserts its PROPORTIONAL share of that bar
/// (rounded UP), so the eight partitions together still prove ≥50 programs were bound. A partition that silently
/// stopped parsing goes RED instead of green-and-empty.</para>
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class StorageFormNistEquivalenceTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost: 171.5 s ÷ 8 ≈ 21 s per collection.</summary>
    public const int Partitions = 8;

    /// <summary>The whole-corpus parse bar the unpartitioned sweep asserted, apportioned below.</summary>
    private const int WholeCorpusMinParsed = 50;

    [PartitionedRowSource(nameof(NistPrograms))]
    public static IEnumerable<object[]> AllNistPrograms() => StorageFormEquivalence.NistPrograms();

    /// <summary>This partition's share of the NIST corpus.</summary>
    public static IEnumerable<object[]> NistPrograms() =>
        TestPartitioning.SliceRows<TSlot>(AllNistPrograms(), Partitions);

    [Fact]
    public void NistCorpus_StorageFormEqualsLegacy()
    {
        var slice = NistPrograms().Select(r => (string)r[0]).ToList();
        int total = AllNistPrograms().Count();
        Assert.NotEmpty(slice);

        int parsed = 0;
        var allDivergences = new List<string>();
        foreach (string p in slice)
        {
            var (divergences, ok) = StorageFormEquivalence.Analyze(p, 85, Path.GetFileNameWithoutExtension(p));
            if (!ok) continue;
            parsed++;
            allDivergences.AddRange(divergences.Select(d => $"{Path.GetFileName(p)}: {d}"));
        }

        // This partition's proportional share of the whole-corpus `parsed >= 50` bar, rounded UP so the
        // partitions together are never weaker than the sweep they replaced.
        int minParsed = ((WholeCorpusMinParsed * slice.Count) + total - 1) / total;
        Assert.True(parsed >= minParsed,
            $"expected ≥{minParsed} of this partition's {slice.Count} NIST programs to parse, got {parsed}");
        Assert.True(allDivergences.Count == 0,
            $"{allDivergences.Count} StorageForm divergence(s) across {parsed} NIST programs:\n"
            + string.Join("\n", allDivergences.Take(15)));
    }
}

// ⛔ THE EIGHT PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover the corpus
// exactly once; a deleted partition class silently drops an eighth of the NIST corpus from this gate without it.

/// <summary>StorageForm NIST partition 0 of
/// <see cref="StorageFormNistEquivalenceTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class StorageFormNistEquivalenceTests_P0 : StorageFormNistEquivalenceTestsBase<Slot0>;

/// <summary>StorageForm NIST partition 1.</summary>
public sealed class StorageFormNistEquivalenceTests_P1 : StorageFormNistEquivalenceTestsBase<Slot1>;

/// <summary>StorageForm NIST partition 2.</summary>
public sealed class StorageFormNistEquivalenceTests_P2 : StorageFormNistEquivalenceTestsBase<Slot2>;

/// <summary>StorageForm NIST partition 3.</summary>
public sealed class StorageFormNistEquivalenceTests_P3 : StorageFormNistEquivalenceTestsBase<Slot3>;

/// <summary>StorageForm NIST partition 4.</summary>
public sealed class StorageFormNistEquivalenceTests_P4 : StorageFormNistEquivalenceTestsBase<Slot4>;

/// <summary>StorageForm NIST partition 5.</summary>
public sealed class StorageFormNistEquivalenceTests_P5 : StorageFormNistEquivalenceTestsBase<Slot5>;

/// <summary>StorageForm NIST partition 6.</summary>
public sealed class StorageFormNistEquivalenceTests_P6 : StorageFormNistEquivalenceTestsBase<Slot6>;

/// <summary>StorageForm NIST partition 7.</summary>
public sealed class StorageFormNistEquivalenceTests_P7 : StorageFormNistEquivalenceTestsBase<Slot7>;
