// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NIST programs whose original GOLDEN was legacy-tainted — the baselined expected file encoded a LEGACY
/// non-conformance. Each pin asserts the SPEC-derived outcome with its ISO citation. The goldens below were
/// RE-BASELINED to the conforming output (owner-approved, DEVLOG 569; <c>tests/nist/corpus.tsv</c> marks each row
/// <c>divergent</c> with its ISO citation), so the programs are ALSO byte-locked in <see cref="NistDifferentialTests"/>;
/// these pins remain as the citation-bearing documentation of WHY each golden diverges from the legacy.
/// <para>ONE pin is of a different KIND and is marked as such: <see cref="NC201A_VaryingAfterFromOuterInductionVariable_RunsEightBodies"/>
/// is not a legacy artefact but a CCVS DEFECT — the CCVS program's own expected value is what the ISO text
/// contradicts, so the conforming compiler must FAIL that check and the golden records the failure. That is the
/// only golden in the corpus allowed to carry one, and the allowance is declared in <c>tests/nist/corpus.tsv</c>
/// and audited by <see cref="CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects"/>.</para>
/// </summary>
public sealed class SpecPinnedNistTests
{
    /// <summary>NC236A: the golden marks SCH-TEST-F1-8 / F1-10 "TEST DELETED" — a legacy ARTIFACT: the legacy's
    /// serial SEARCH with <c>VARYING index-of-ANOTHER-table</c> (ISO §14.9.37.4 GR3 c) 2.) falls through to the CCVS
    /// DE-LETE paragraph instead of executing the scan (verified by running the legacy directly, DEVLOG 533).
    /// Per GR3 c) 2. the scan uses the searched table's own first index and varies the other index in step — both
    /// tests then PASS: the conforming run executes 010 OF 010 with nothing deleted.</summary>
    [Fact]
    public void NC236A_SearchVaryingOtherTableIndex_ExecutesAllTests()
    {
        string src = TestRepo.Nist("programs", "NC236A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC236A.dll"), NistTestName: "NC236A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC236A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc236a.txt"));

            Assert.Contains("PASS  SCH-TEST-F1-8", output);
            Assert.Contains("PASS  SCH-TEST-F1-10", output);
            Assert.Contains("010 OF 010  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>NC235A: the golden marks IDX-TEST-F2-9 / F2-12 "TEST DELETED" — a legacy artifact: its SEARCH ALL
    /// over an OCCURS-DEPENDING table with a CONDITION-NAME WHEN (<c>WHEN LASTA (IDX-1)</c>) fell through to the
    /// CCVS DE-LETE paragraph. Per ISO §14.9.37 Format 2 (a WHEN condition-name over a table element keyed by the
    /// search index) + §13.18.38 GR7 (the table's count IS data-name-1's value) both tests execute and PASS — the
    /// conforming run is 013 OF 013 with nothing deleted.</summary>
    [Fact]
    public void NC235A_SearchAllConditionNameOverOdo_ExecutesAllTests()
    {
        string src = TestRepo.Nist("programs", "NC235A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC235A.dll"), NistTestName: "NC235A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("; ", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC235A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc235a.txt"));

            Assert.Contains("PASS  IDX-TEST-F2-9", output);
            Assert.Contains("PASS  IDX-TEST-F2-12", output);
            Assert.Contains("013 OF 013  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>ST127A at <c>--std 2002</c> — the FREE, EXTERNALLY-AUTHORED witness for kb/Work PB704, and the
    /// one the NIST leg structurally cannot give. <c>ST127A.cob:257</c> writes <c>WITH DUPLICATES IN ORDER</c>
    /// (ISO §14.9.40.2), the only occurrence of the phrase anywhere in the tree, and it passed for two trains
    /// only because the NIST leg runs at <c>--std 85</c>, where §8.9 does not yet reserve ORDER: the leg is
    /// evidence about COBOL-85 and about nothing else (feedback_measure_the_selectors_complement). While the
    /// phrase's own word rode <c>cobolWord</c>, the §8.9 funnel — which screens IDENTIFIER occurrences
    /// POSITION-BLIND — refused it as a user-defined word from 2002 on, so a real CCVS program was rejected at
    /// every edition a user is likely to target.
    /// <para>⚠ THE ASSERTION IS THE ABSENCE OF COBOLNET0901, NOT A CLEAN COMPILE, and deliberately so. ST127A is
    /// an X3.23-1985 program: at strict COBOL-2002 its SD still carries the DATA RECORDS clause, which ISO 2002
    /// DELETED (COBOLNET0873), so a "compiles clean" assertion could only be made under <c>--permissive</c> —
    /// and under <c>--permissive</c> the reservation gate does not fire and the 0901 degrades to a warning, so
    /// that spelling of the test would have passed BEFORE the fix as well (feedback_green_gates_arent_evidence).
    /// The strict compile with 0901 excluded fails on the old compiler and passes on the new one.</para></summary>
    [Fact]
    public void ST127A_SortDuplicatesInOrder_IsNotAReservedWordViolationAt2002()
    {
        string src = TestRepo.Nist("programs", "ST127A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "ST127A.dll"), NistTestName: "ST127A", DialectLevel: 2002));
            string all = string.Join("\n", r.Errors);
            Assert.DoesNotContain("COBOLNET0901", all, StringComparison.Ordinal);
            Assert.DoesNotContain("'ORDER'", all, StringComparison.Ordinal);
            // The program's ONE remaining strict-2002 obstacle is the deleted FD/SD clause, not the SORT phrase —
            // asserted so a future change that silently stops compiling the SORT at all cannot pass this test.
            Assert.Contains("COBOLNET0873", all, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>NC201A PFM-TEST-F4-23 — the one golden in the corpus that records a FAILING CCVS test, and the
    /// only one where the CCVS PROGRAM'S OWN EXPECTATION is what the ISO text contradicts (every other
    /// re-baselined golden corrected a LEGACY non-conformance and stayed all-PASS).
    /// <para>The CCVS check, titled "ORDER OF INITIALISATION OF VARYING IDENTIFIERS" and referenced to the 1985
    /// text as <c>VI-114 6.20.4 GR10(d)1</c>, executes
    /// <c>PERFORM PFM-F4-23-PROC VARYING PFM-A1 FROM 1 BY 1 UNTIL PFM-A1 &gt; 3 AFTER PFM-B1 FROM PFM-A1 BY 1
    /// UNTIL PFM-B1 &gt; 3</c> and asserts the body ran SIX times — i.e. that PFM-A1 is augmented BEFORE PFM-B1
    /// is reset from it (3+2+1). ISO §14.9.28.4 GR13 e) 2's true branch orders the two the other way: "a. the
    /// induction variable associated with the current condition is set to its initialization value", then "b. the
    /// condition to the left … becomes the current condition, and c. the induction variable associated with the
    /// new current condition is incremented". With GR12 re-identifying the FROM operand at every setting
    /// operation and GR13's closing paragraph giving the change immediate effect, PFM-B1 is reset from the
    /// PRE-augment PFM-A1 and the body runs EIGHT times (3+3+2). Verified against the canonical PDF page 718,
    /// which carries the rule as prose with no figure. Per CLAUDE.md rule 1 the ISO text is the oracle and the
    /// CCVS corpus is a regression net, so the golden records the FAIL and the divergence is declared
    /// CCVS-DEFECT in <c>tests/nist/corpus.tsv</c>, audited by
    /// <see cref="CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects"/> and
    /// determined in <c>docs/CONFORMANCE.md</c> §3. kb/Work PB436.</para>
    /// <para>⚠ THE ASSERTION IS THE COMPUTED VALUE, NOT THE FAIL LINE. <c>NistDifferentialTests</c> masks the
    /// <c>COMPUTED=</c> operand on both sides of its byte comparison, so the golden alone cannot pin the number
    /// WiseOwl COBOL actually produced — a lowering that ran the body nine times would still match it. This pin
    /// reads the digits.</para></summary>
    [Fact]
    public void NC201A_VaryingAfterFromOuterInductionVariable_RunsEightBodies()
    {
        string src = TestRepo.Nist("programs", "NC201A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC201A.dll"), NistTestName: "NC201A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC201A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc201a.txt"));

            Assert.Contains("FAIL* PFM-TEST-F4-23", output, StringComparison.Ordinal);
            Assert.Contains("COMPUTED=  000000000000000008", output, StringComparison.Ordinal);
            Assert.Contains("CORRECT =  000000000000000006", output, StringComparison.Ordinal);
            // And nothing ELSE in NC201A regressed: exactly one of its 59 checks fails, and it is this one.
            Assert.Contains("058 OF 059  TESTS WERE EXECUTED SUCCESSFULLY", output, StringComparison.Ordinal);
            Assert.Contains("001 TEST(S) FAILED", output, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
