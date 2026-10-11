// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The VERSION TEST MATRIX catalogue (docs/VERSION_TEST_MATRIX_DESIGN.md; Phase 1) and its row generators — the
/// data half of the matrix, held in ONE non-generic type so the twelve partitions of
/// <see cref="VersionMatrixTestsBase{TSlot}"/> share a single parse of <c>constructs.json</c> rather than one per
/// closed generic type.
/// </summary>
/// <remarks>
/// The catalogue is the CANONICAL <c>tests/version-matrix/constructs.json</c> (design §10 #5 — the
/// VERSION_CHANGE_REFERENCE.md tables and the future in-code registry are renderings of that file; extend it,
/// never fork).
/// </remarks>
internal static class VersionMatrixCatalogue
{
    internal sealed record Construct(string Id, string Description, int IntroducedIn, int? RemovedIn, string Vcr,
        string Source, string Status, string? ExpectDiagnostic, int? ObsoleteIn, string? ExpectDiagnosticBelow,
        IReadOnlyDictionary<string, string>? Copybooks = null);

    /// <summary>The catalogue file: the one spelling of the path, which the architecture oracle also records as an
    /// input (<see cref="ArchOracle.RowSource"/>). Declared before <see cref="All"/>, whose initializer reads it.</summary>
    internal static string CataloguePath { get; } = TestRepo.VersionMatrix("constructs.json");

    internal static IReadOnlyList<Construct> All { get; } = LoadCatalogue();

    /// <summary>Id → construct. The theories look a construct up per ROW (2,127 of them), so this is a dictionary
    /// rather than the linear <c>First(…)</c> scan it replaced.</summary>
    internal static IReadOnlyDictionary<string, Construct> ById { get; } =
        All.ToDictionary(c => c.Id, StringComparer.Ordinal);

    internal static IEnumerable<Construct> Active => All.Where(c => c.Status == "active");

    /// <summary>The matrix expected-outcome function f(case, V) (design §2).</summary>
    internal static bool ExpectCompiles(Construct c, int edition)
        => edition >= c.IntroducedIn && (c.RemovedIn is null || edition < c.RemovedIn);

    /// <summary>Every (active construct × edition) cell — the introduction-gating matrix, both ways.</summary>
    internal static IEnumerable<object[]> Matrix()
    {
        // status:"pending" rows are catalogued (their edition metadata is frozen by the registry drift tests)
        // but not yet implemented — their compile assertions activate when the owning roadmap phase lands.
        foreach (var c in Active)
        {
            foreach (int v in EditionHarness.Editions)
            {
                yield return [c.Id, v];
            }
        }
    }

    /// <summary>Every (active construct × edition BELOW its introduction) cell.</summary>
    internal static IEnumerable<object[]> IntroducedMatrix()
    {
        foreach (var c in Active)
        {
            foreach (int v in EditionHarness.Editions.Where(v => v < c.IntroducedIn))
            {
                yield return [c.Id, v];
            }
        }
    }

    /// <summary>Every (removed construct × edition ≥ removedIn) cell.</summary>
    internal static IEnumerable<object[]> RemovedMatrix()
    {
        foreach (var c in Active.Where(c => c.RemovedIn is not null))
        {
            foreach (int v in EditionHarness.Editions.Where(v => v >= c.RemovedIn))
            {
                yield return [c.Id, v];
            }
        }
    }

    /// <summary>Every (obsolete-flagged construct × edition inside its availability window) cell.</summary>
    internal static IEnumerable<object[]> ObsoleteMatrix()
    {
        foreach (var c in Active.Where(c => c.ObsoleteIn is not null))
        {
            foreach (int v in EditionHarness.Editions.Where(v => v >= c.IntroducedIn
                                                                 && v < (c.RemovedIn ?? int.MaxValue)))
            {
                yield return [c.Id, v];
            }
        }
    }

    /// <summary>The continuity witness set × later editions: every corpus green∪divergent program crossed with
    /// 2002/2014/2023.</summary>
    internal static IEnumerable<object[]> ContinuityCells() =>
        from name in CorpusManifest.Green().Select(r => r.Name)
        from edition in new[] { 2002, 2014, 2023 }
        select new object[] { name, edition };

    private static IReadOnlyList<Construct> LoadCatalogue()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(CataloguePath));
        var list = new List<Construct>();
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
        {
            list.Add(new Construct(
                e.GetProperty("id").GetString()!,
                e.GetProperty("description").GetString()!,
                e.GetProperty("introducedIn").GetInt32(),
                e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
                e.GetProperty("vcr").GetString()!,
                e.GetProperty("source").GetString()!,
                e.TryGetProperty("status", out var s) ? s.GetString()! : "active",
                e.TryGetProperty("expectDiagnostic", out var d) ? d.GetString() : null,
                e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
                e.TryGetProperty("expectDiagnosticBelow", out var db) ? db.GetString() : null,
                // A row whose program COPYs library text carries that text (file name -> content): it is staged
                // beside the program and named as a --copy directory (kb/Work PB1355 — a missing text is CBL3620).
                e.TryGetProperty("copybooks", out var cb)
                    ? cb.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal)
                    : null));
        }

        return list;
    }
}

/// <summary>
/// VERSION TEST MATRIX (docs/VERSION_TEST_MATRIX_DESIGN.md; Phase 1). Tests the compiler as N per-ISO-edition
/// compilers: a (construct × target-edition) matrix whose expected outcome is COMPUTED from each construct's
/// edition metadata — compiles iff <c>introducedIn ≤ V</c> and not removed by V (the design's <c>f(case, V)</c>).
/// Compilation goes through <see cref="EditionHarness"/> — the one per-edition compile path.
///
/// Covered today: INTRODUCTION-GATING both ways (INV-2) and CONTINUITY (INV-1, the FULL corpus witness set).
/// Not yet: removed-construct gating and behavior variants (INV-3) — those await the EditionValidator (Phase 2);
/// their rows join constructs.json with <c>removedIn</c>/variant metadata as that lands.
/// </summary>
/// <remarks>
/// ⛔ THIS CLASS IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). It held <b>2,127 theory rows
/// running SERIALLY for 720.5 s</b> on the battery #41 trx, i.e. essentially the WHOLE 721 s Conformance leg was
/// this one class on one of 32 cores, because xUnit 2.9.2 makes each test CLASS one collection. The rows are
/// declared ONCE here and sliced <c>index % <see cref="Partitions"/></c> ways by the concrete
/// <c>VersionMatrixTests_P0 … _P11</c> below, each of which is its own collection. The partitions keep
/// <c>VersionMatrixTests</c> in their names so every existing <c>FullyQualifiedName~VersionMatrixTests</c> filter
/// (plan §0, CI's shard matrix, <c>.claude/skills/gate</c>) still selects them unchanged.
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class VersionMatrixTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost: 720.5 s ÷ 12 ≈ 60 s per collection, which
    /// is at the assembly's own throughput floor (1,948 s of test time ÷ 32 cores ≈ 61 s), so a thirteenth
    /// partition would buy nothing.</summary>
    public const int Partitions = 12;

    [PartitionedRowSource(nameof(Matrix))]
    public static IEnumerable<object[]> AllMatrix() => VersionMatrixCatalogue.Matrix();

    /// <summary>This partition's share of the (construct × edition) matrix.</summary>
    public static IEnumerable<object[]> Matrix() => TestPartitioning.SliceRows<TSlot>(AllMatrix(), Partitions);

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Construct_MatchesEditionExpectation(string constructId, int edition)
    {
        var c = VersionMatrixCatalogue.ById[constructId];
        bool expectCompiles = VersionMatrixCatalogue.ExpectCompiles(c, edition);
        var (ok, diagnostics) = EditionHarness.Compile(c.Source, edition, c.Copybooks);

        if (expectCompiles)
        {
            Assert.True(ok, $"[{constructId}] expected to COMPILE at COBOL-{edition} (introduced {c.IntroducedIn}"
                + $"{(c.RemovedIn is { } r ? $", removed {r}" : "")}; {c.Vcr}) but failed:\n{string.Join("\n", diagnostics)}");
        }
        else
        {
            Assert.False(ok, $"[{constructId}] expected to be REJECTED at COBOL-{edition} (not valid until "
                + $"{c.IntroducedIn}{(c.RemovedIn is { } r ? $" / removed {r}" : "")}; {c.Vcr}) but it compiled.");
            // The two-obligation rule's diagnostic half: reject cells assert the QUALITY of the rejection
            // once their edition-band code exists (P2.7 — expectDiagnostic in constructs.json). A DUAL-window
            // row (introduced ≥2002 AND removed — e.g. EXIT METHOD, method WS) rejects with DIFFERENT codes at
            // the two edges: expectDiagnosticBelow (0900 not-yet-introduced) below introducedIn,
            // expectDiagnostic (0902 removed) at/after removedIn (the exit-method-window reactivation
            // contract, landed with OO slice 2).
            string? code = edition < c.IntroducedIn ? c.ExpectDiagnosticBelow ?? c.ExpectDiagnostic : c.ExpectDiagnostic;
            if (code is not null)
            {
                EditionHarness.AssertHasDiagnostic(diagnostics, code);
            }
        }
    }

    [PartitionedRowSource(nameof(IntroducedMatrix))]
    public static IEnumerable<object[]> AllIntroducedMatrix() => VersionMatrixCatalogue.IntroducedMatrix();

    /// <summary>This partition's share of the below-introduction cells.</summary>
    public static IEnumerable<object[]> IntroducedMatrix() =>
        TestPartitioning.SliceRows<TSlot>(AllIntroducedMatrix(), Partitions);

    /// <summary>The introduction contract's SECOND axis (CA14): a construct the targeted edition has NOT YET
    /// acquired is an error under <c>--permissive</c> too, never a warning. <c>--permissive</c> is the §10 #1
    /// MIGRATION mode — it exists so a program written against an older edition still compiles against a newer
    /// one, which is meaningful only for constructs an edition REMOVED. No pre-existing program can legally
    /// contain a construct its own edition had not yet acquired, so there is nothing to migrate and nothing to
    /// be lenient about; <see cref="EditionSeverityPolicy"/> already says so
    /// (<c>For(NotYetIntroduced) =&gt; Error</c> on both axes).
    /// <para>⛔ THIS TEST IS THE REASON THE POLICY IS NOW AUTOMATIC. Until CA14 the permissive axis was tested
    /// ONLY for removal rows, so the ONE site that routed an introduction through the removed-severity seam
    /// (SYNCHRONIZED on a group item) accepted it with a warning under <c>--permissive</c> for two phases with
    /// nothing red. A new introduction row joins this theory the moment it enters constructs.json.</para></summary>
    [Theory]
    [MemberData(nameof(IntroducedMatrix))]
    public void IntroducedConstruct_IsRejectedUnderPermissive(string constructId, int edition)
    {
        var c = VersionMatrixCatalogue.ById[constructId];
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition, permissive: true, copybooks: c.Copybooks);
        Assert.False(ok, $"[{constructId}] is a COBOL-{c.IntroducedIn} introduction and must be REJECTED at "
            + $"COBOL-{edition} under --permissive as well as strict — permissive is the migration mode for "
            + $"REMOVED constructs, not a licence for future ones ({c.Vcr}). It compiled"
            + (warnings.Count > 0 ? $", warning only:\n{string.Join("\n", warnings)}" : " clean."));
        // Same code selection as the strict theory: a dual-window row rejects with its below-edge code.
        if ((c.ExpectDiagnosticBelow ?? c.ExpectDiagnostic) is { } code)
        {
            EditionHarness.AssertHasDiagnostic(errors, code);
        }
    }

    [PartitionedRowSource(nameof(RemovedMatrix))]
    public static IEnumerable<object[]> AllRemovedMatrix() => VersionMatrixCatalogue.RemovedMatrix();

    /// <summary>This partition's share of the removed-construct cells.</summary>
    public static IEnumerable<object[]> RemovedMatrix() =>
        TestPartitioning.SliceRows<TSlot>(AllRemovedMatrix(), Partitions);

    /// <summary>The §10 #1 migration contract (P2.7): a construct the targeted edition REMOVED must COMPILE
    /// under <c>--permissive</c> — with its edition-band diagnostic carried as a WARNING — preserving the
    /// pre-removal semantics. One theory row per (removed-construct × edition ≥ removedIn).</summary>
    [Theory]
    [MemberData(nameof(RemovedMatrix))]
    public void RemovedConstruct_CompilesPermissive_WithWarning(string constructId, int edition)
    {
        var c = VersionMatrixCatalogue.ById[constructId];
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition, permissive: true, copybooks: c.Copybooks);
        Assert.True(ok, $"[{constructId}] permissive at COBOL-{edition} must COMPILE (the §10 #1 migration "
            + $"contract): {string.Join("\n", errors)}");
        if (c.ExpectDiagnostic is { } code)
        {
            EditionHarness.AssertHasDiagnostic(warnings, code);
        }
    }

    [PartitionedRowSource(nameof(ObsoleteMatrix))]
    public static IEnumerable<object[]> AllObsoleteMatrix() => VersionMatrixCatalogue.ObsoleteMatrix();

    /// <summary>This partition's share of the obsolete-flag cells.</summary>
    public static IEnumerable<object[]> ObsoleteMatrix() =>
        TestPartitioning.SliceRows<TSlot>(AllObsoleteMatrix(), Partitions);

    /// <summary>The archaic/obsolete flag contract (P2.6, ISO §4.2.12/§4.2.13): an <c>obsoleteIn</c> row
    /// COMPILES throughout its availability window (the element remains conforming) and carries the FIXED
    /// 0903 band WARNING exactly at editions ≥ obsoleteIn — never below (no NIST-85 noise). A DUAL row
    /// (obsolete then later REMOVED — QUOTE→numeric: obsolete 2014 per Annex E.2 item 21, removed 2023) is
    /// bounded below its <c>removedIn</c>; the removal edge is the ValidAt/RemovedMatrix theories' job and
    /// carries the row's <c>expectDiagnostic</c> (0902), while the obsolete verdict's code is ALWAYS 0903
    /// (<c>ConstructRegistry.Check</c> emits <c>EditionCodes.ObsoleteFlag</c> for every Obsolete verdict).</summary>
    [Theory]
    [MemberData(nameof(ObsoleteMatrix))]
    public void ObsoleteConstruct_CompilesEverywhere_WarnsFromObsoleteEdition(string constructId, int edition)
    {
        var c = VersionMatrixCatalogue.ById[constructId];
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition, copybooks: c.Copybooks);
        Assert.True(ok, $"[{constructId}] must COMPILE at COBOL-{edition} (archaic ≠ removed): {string.Join("\n", errors)}");
        if (edition >= c.ObsoleteIn)
        {
            EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0903");
        }
        else
        {
            EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
        }
    }

    [PartitionedRowSource(nameof(ContinuityCells))]
    public static IEnumerable<object[]> AllContinuityCells() => VersionMatrixCatalogue.ContinuityCells();

    /// <summary>This partition's share of the continuity witness set × later editions.</summary>
    public static IEnumerable<object[]> ContinuityCells() =>
        TestPartitioning.SliceRows<TSlot>(AllContinuityCells(), Partitions);

    /// <summary>INV-1 (continuity), RESTATED at the P2.7 flip (the §10 #1 migration posture) and PROMOTED at rearch
    /// P3 step 8 from a 13-row seed to the FULL in-process sweep — the authoritative gate, replacing the out-of-band
    /// bash sweep (kept only as a CLI convenience). Runs over the corpus witness set (every green∪divergent program
    /// compiles at 85 by definition) × {2002,2014,2023} via the SAME <c>[Theory][MemberData]</c> mechanism as
    /// <see cref="NistDifferentialTestsBase{TSlot}.NistProgram_MatchesGolden"/>. A program that compiles at 85 must
    /// STILL compile at each later edition **UNDER PERMISSIVE** — a permissive break is a regression. Under STRICT a
    /// rejection is legitimate (the removed '85 elements every NIST program carries — LABEL RECORDS in every FD, …)
    /// but must trace to a recognized edition-band code (COBOLNET08xx/09xx), never a generic parse error. Uses
    /// <c>CheckOnly</c> (parse + edition-validate + bind, no Roslyn backend — the verdict is settled pre-backend,
    /// DEVLOG 627).</summary>
    [Theory]
    [MemberData(nameof(ContinuityCells))]
    public void Cobol85Program_StillCompilesAtLaterEdition(string testName, int edition)
    {
        // Permissive: the §10 #1 migration contract — a documented removal warns, never breaks.
        var (permOk, permDiag) = EditionHarness.CompileNist(testName, edition, permissive: true, checkOnly: true);
        Assert.True(permOk, $"[INV-1 continuity] {testName} (compiles at 85) failed PERMISSIVE at COBOL-{edition}; "
            + $"permissive must accept documented removals with warnings (§10 #1) — this is a regression:\n"
            + string.Join("\n", permDiag));

        // Strict: a rejection is legitimate, but a rejection with NO edition-band code (a generic COBOL0001 / a
        // non-edition bind error) is the co-equal-diagnostic violation.
        var (strictOk, strictDiag) = EditionHarness.CompileNist(testName, edition, permissive: false, checkOnly: true);
        if (!strictOk)
        {
            Assert.True(strictDiag.Any(d => VersionMatrixTests.EditionBandCode.IsMatch(d)),
                $"[INV-1 diagnosis] {testName} strict@COBOL-{edition} was rejected WITHOUT a recognized edition-band "
                + "code (COBOLNET08xx/09xx):\n" + string.Join("\n", strictDiag));
        }
    }
}

// ⛔ THE TWELVE PARTITIONS. Each is its own xUnit collection, which is the entire point — see
// TestPartitioning for why xUnit v2 offers no other lever, and TestPartitionAudit for the drift gate that
// proves these twelve cover every row of every theory above exactly once. Adding or removing one WITHOUT
// changing VersionMatrixTestsBase<>.Partitions is a RED, not a silent row drop.
/// <summary>Version-matrix partition 0 of <see cref="VersionMatrixTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class VersionMatrixTests_P0 : VersionMatrixTestsBase<Slot0>;

/// <summary>Version-matrix partition 1.</summary>
public sealed class VersionMatrixTests_P1 : VersionMatrixTestsBase<Slot1>;

/// <summary>Version-matrix partition 2.</summary>
public sealed class VersionMatrixTests_P2 : VersionMatrixTestsBase<Slot2>;

/// <summary>Version-matrix partition 3.</summary>
public sealed class VersionMatrixTests_P3 : VersionMatrixTestsBase<Slot3>;

/// <summary>Version-matrix partition 4.</summary>
public sealed class VersionMatrixTests_P4 : VersionMatrixTestsBase<Slot4>;

/// <summary>Version-matrix partition 5.</summary>
public sealed class VersionMatrixTests_P5 : VersionMatrixTestsBase<Slot5>;

/// <summary>Version-matrix partition 6.</summary>
public sealed class VersionMatrixTests_P6 : VersionMatrixTestsBase<Slot6>;

/// <summary>Version-matrix partition 7.</summary>
public sealed class VersionMatrixTests_P7 : VersionMatrixTestsBase<Slot7>;

/// <summary>Version-matrix partition 8.</summary>
public sealed class VersionMatrixTests_P8 : VersionMatrixTestsBase<Slot8>;

/// <summary>Version-matrix partition 9.</summary>
public sealed class VersionMatrixTests_P9 : VersionMatrixTestsBase<Slot9>;

/// <summary>Version-matrix partition 10.</summary>
public sealed class VersionMatrixTests_P10 : VersionMatrixTestsBase<Slot10>;

/// <summary>Version-matrix partition 11.</summary>
public sealed class VersionMatrixTests_P11 : VersionMatrixTestsBase<Slot11>;

/// <summary>
/// The version matrix's WHOLE-CATALOGUE facts — the assertions that are about the catalogue itself rather than
/// about one (construct × edition) cell, so they must run ONCE and are deliberately NOT on the partitioned base
/// (an inherited <c>[Fact]</c> would run twelve times and inflate the count twelvefold).
/// </summary>
public sealed class VersionMatrixTests
{
    /// <summary>A recognized edition diagnostic (the 0900–0903 band or any pinned 08xx gate, plus the 15xx
    /// conformance-requirement band) — as opposed to a generic <c>COBOL0001</c> parse error. The point is "a COBOLNET
    /// code diagnosed the edition delta." The 15xx band is included because a version-conditioned structural SR read
    /// directly in the binder (the CheckDigitCapacity/binder-reads-edition doctrine — e.g. VCR 18/31's external-file
    /// FILE-STATUS/RELATIVE-KEY consistency, COBOLNET1573/1575) reports in that band, not 08xx/09xx: for a CONTINUITY
    /// witness (green at 85 by construction), ANY 15xx ERROR surfacing only at a later edition is necessarily
    /// edition-conditioned — an edition-invariant 15xx SR would have errored at 85 too and excluded the program from
    /// the green set — so it is a legitimate recognized-edition rejection, never a generic parse error.</summary>
    internal static readonly System.Text.RegularExpressions.Regex EditionBandCode =
        new(@"COBOLNET(0[89]|15)\d\d", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Every pending row must carry its activation contract: an owning-phase note in the description
    /// and edition metadata good enough to freeze — pending is a scheduling state, never a metadata hole.</summary>
    [Fact]
    public void PendingRows_AreCataloguedWithActivationContracts()
    {
        foreach (var c in VersionMatrixCatalogue.All.Where(c => c.Status == "pending"))
        {
            Assert.Contains("PENDING", c.Description, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(c.Vcr), $"{c.Id}: pending row without a vcr citation");
            Assert.False(string.IsNullOrWhiteSpace(c.Source), $"{c.Id}: pending row without a source program");
        }

        Assert.All(VersionMatrixCatalogue.All,
            c => Assert.True(c.Status is "active" or "pending", $"{c.Id}: bad status '{c.Status}'"));
    }

    /// <summary>The ST representative is a DOCUMENTED removal, not a continuity witness: NIST programs carry
    /// obsolete '85 elements DELETED by ISO/IEC 1989:2002 — ST101A's FDs write LABEL RECORDS (0902, the
    /// validator's first gate, DEVLOG 588) and its SD writes DATA RECORDS (0873, still binder-side) — so it
    /// must compile at 85 and REJECT at 2002+ STRICT with a recognized edition-band code. The validator
    /// fail-fasts BEFORE Emit, so today only the 0902s surface; once P2.6 migrates the SD 0873 gate into the
    /// validator (one enforcement site), BOTH appear in the one pass — re-assert 0873 then. Under PERMISSIVE
    /// it must COMPILE (the removals warn), which the continuity theory above already witnesses.</summary>
    [Fact]
    public void St101A_DocumentedRemovals_RejectStrictAt2002Plus()
    {
        var (ok85, _) = EditionHarness.CompileNist("ST101A", 85);
        Assert.True(ok85, "ST101A must compile at --std 85");
        var (ok, diagnostics) = EditionHarness.CompileNist("ST101A", 2023);
        Assert.False(ok, "ST101A's removed '85 elements must be rejected at COBOL-2023 strict");
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET0902"));   // the FD LABEL RECORDS gate
        // Both removed elements surface in the ONE validator pass now that the SD DATA RECORDS 0873 gate
        // migrated validator-side (P2.6 / Table-7 row 7.1 — the fail-fast no longer hides it behind Emit).
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET0873"));
    }
}
