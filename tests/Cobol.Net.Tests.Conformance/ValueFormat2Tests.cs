// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Format 2 (table) VALUE clause (ISO/IEC 1989:2023 §13.18.63.2, COBOL-2002): a literal list keyed to occurrence
/// subscripts by a MANDATORY FROM (subscript) phrase with an optional TO. Per-occurrence initialization — the
/// odometer fill (GR12), cyclic literal reuse under TO (GR13), no-TO = fill to the maximum (GR14), later-FROM-wins on
/// overlap (GR15), and the dynamic-capacity computation (GR16) — over the WHOLE population §13.18.63.3 SR18 admits:
/// the subject's own OCCURS entry, an entry SUBORDINATE to an OCCURS entry, and any number of dimensions.
/// The geometry rules SR18 / SR20 / SR21 / SR22 / SR23 are each asserted here, in both directions (kb/Work PB505:
/// one staged COBOLNET0899 used to answer for all of them at once, refusing the conforming shapes alongside the
/// violations). The paired glued-multi-literal reject (COBOLNET1585) closes the Format-1 gluing defect.
/// Per §13.18.63.4 the initial value of occurrences OUTSIDE every FROM..TO range is UNDEFINED — never asserted here.
/// </summary>
public sealed class ValueFormat2Tests
{
    private static string Prog(string ws, string body) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. VF2.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + ws + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
        """ + "\n" + body + "\n" + """
            STOP RUN.
        """;

    // ── Fixed tables ──

    /// <summary>GR14 (no TO = fill to the maximum) + GR13 (cyclic reuse): six occurrences, two literals ⇒
    /// AAA BBB AAA BBB AAA BBB.</summary>
    [Fact]
    public void FixedTable_NoTo_CyclicFill()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X(3) OCCURS 6 VALUES ARE \"AAA\" \"BBB\" FROM (1).",
                 "    DISPLAY \"[\" E(1) E(2) E(3) E(4) E(5) E(6) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AAABBBAAABBBAAABBB]", stdout);
    }

    /// <summary>GR12/GR13 explicit sub-range: only occurrences 2..4 are keyed (AA,BB,AA cyclic); occurrences outside
    /// the range are NOT asserted (§13.18.63.4 leaves them undefined).</summary>
    [Fact]
    public void FixedTable_ExplicitRange()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X(2) OCCURS 5 VALUES ARE \"AA\", \"BB\" FROM (2) TO (4).",
                 "    DISPLAY \"[\" E(2) E(3) E(4) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AABBAA]", stdout);
    }

    /// <summary>GR15 (later FROM wins on overlap): "A" over 1..4 then "Z" over 2..3 ⇒ A Z Z A.</summary>
    [Fact]
    public void FixedTable_LastFromWins()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 4 VALUES ARE \"A\" FROM (1) TO (4) \"Z\" FROM (2) TO (3).",
                 "    DISPLAY \"[\" E(1) E(2) E(3) E(4) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AZZA]", stdout);
    }

    /// <summary>A numeric element table: N(1..3) = 1,2,3 (GR12 sequential).</summary>
    [Fact]
    public void FixedTable_NumericElement()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 N PIC 9 OCCURS 3 VALUES ARE 1 2 3 FROM (1).",
                 "    DISPLAY \"[\" N(1) N(2) N(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[123]", stdout);
    }

    // ── Dynamic-capacity table (Annex D.3.7) ──

    /// <summary>GR16a + Annex D.3.7: <c>OCCURS DYNAMIC FROM 1 TO 20 VALUES ARE "Leeds","Bordeaux","Pisa" FROM (1) TO
    /// (3)</c> opens the table at initial capacity 3 with the three names present — the ledger reject-valid-input
    /// proof (this exact clause was a raw parse error before).</summary>
    [Fact]
    public void DynamicTable_AnnexD37()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 TOWN-NAME PIC X(9) OCCURS DYNAMIC FROM 1 TO 20\n"
               + "      VALUES ARE \"Leeds\" \"Bordeaux\" \"Pisa\" FROM (1) TO (3).",
                 "    DISPLAY \"[\" TOWN-NAME(1) \"|\" TOWN-NAME(2) \"|\" TOWN-NAME(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[Leeds    |Bordeaux |Pisa     ]", stdout);
    }

    // ── The glued-multi-literal reject (the paired defect fix) ──

    /// <summary>A Format-1 data-item VALUE takes exactly ONE literal (§13.18.63.2); a bare list with no FROM was
    /// silently GLUED into one corrupt value. Now rejected loud (COBOLNET1585).</summary>
    [Theory]
    [InlineData("01 W PIC 9(3) VALUE 1 2 3.")]
    [InlineData("01 W PIC XX VALUE \"A\" \"B\".")]
    public void GluedMultiLiteral_Rejected1585(string ws)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(ws, "    DISPLAY \"X\"."), 2023);
        Assert.False(ok, "a bare multi-literal Format-1 VALUE must be rejected (ISO §13.18.63.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1585");
    }

    /// <summary>A single-literal Format-1 VALUE still compiles clean (the reject did not over-fire).</summary>
    [Fact]
    public void SingleLiteralValue_StillCompiles()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 W PIC 9(3) VALUE 42.", "    DISPLAY W."), 2023);
        Assert.True(ok, $"a single-literal VALUE must still compile:\n{string.Join("\n", diag)}");
    }

    // ── Syntax rules ──

    /// <summary>SR20 — a FROM subscript beyond the table maximum is rejected (COBOLNET1586).</summary>
    [Fact]
    public void SubscriptOutOfRange_Rejected1586()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 3 VALUE \"A\" FROM (4).", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1586");
    }

    /// <summary>SR21 — a TO subscript less than its FROM is rejected (COBOLNET1587).</summary>
    [Fact]
    public void ToLessThanFrom_Rejected1587()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 5 VALUES ARE \"A\" \"B\" FROM (4) TO (2).", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1587");
    }

    // ── The subscript TUPLE: §13.18.63.3 SR18/SR20/SR21/SR22/SR23 and the §13.18.63.4 GR12 odometer ──

    /// <summary>§13.18.63.4 GR12's ODOMETER over two dimensions: "Consecutive table elements are referenced by
    /// incrementing by 1 the subscript that represents the least inclusive dimension of the table. When any
    /// reference to a subscript, prior to incrementing it, is equal to the maximum number of occurrences … that
    /// subscript is set to 1 and the subscript for the next most inclusive dimension of the table is incremented
    /// by 1." Six literals over a 2×3 table therefore fill (1 1)…(1 3) then (2 1)…(2 3) — 1 2 3 / 4 5 6.
    /// <para>This case USED to be the compiler's documented gap: the whole multi-dimension population exited
    /// through one COBOLNET0899, and the test that stood here pinned that STAGE (kb/Work PB505, the
    /// [[green_test_can_hold_a_gap_open]] shape — four syntax rules looked covered from the test side because a
    /// green test asserted the refusal).</para></summary>
    [Fact]
    public void MultiDimension_OdometerFill()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 R OCCURS 2.\n      05 C PIC 9 OCCURS 3 VALUES ARE 1 2 3 4 5 6 FROM (1 1) TO (2 3).",
                 "    DISPLAY \"[\" C(1 1) C(1 2) C(1 3) \"|\" C(2 1) C(2 2) C(2 3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[123|456]", stdout);
    }

    /// <summary>§13.18.63.4 GR13's cyclic reuse ACROSS the odometer's carry: two literals from (1 2) to (2 2) on a
    /// 2×3 table visit (1 2) (1 3) (2 1) (2 2) — A B A B. A per-dimension range would have visited four elements
    /// of a rectangle instead, which is not what GR12's fill order is.</summary>
    [Fact]
    public void MultiDimension_CyclicReuseAcrossTheCarry()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 R OCCURS 2.\n      05 C PIC X OCCURS 3 VALUES ARE \"A\" \"B\" FROM (1 2) TO (2 2).",
                 "    DISPLAY \"[\" C(1 2) C(1 3) C(2 1) C(2 2) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[ABAB]", stdout);
    }

    /// <summary>SR18 admits a table VALUE on an entry "subordinate to a data description entry that contains an
    /// OCCURS clause" — CONFORMING source that the staged refusal used to reject. GR14 fills to the maximum and
    /// GR13 reuses the two literals cyclically: AB CD AB.</summary>
    [Fact]
    public void SubordinateToOccurs_IsConforming_Sr18()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T OCCURS 3.\n   05 X PIC X(2) VALUES ARE \"AB\" \"CD\" FROM (1).",
                 "    DISPLAY \"[\" X(1) \"|\" X(2) \"|\" X(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AB|CD|AB]", stdout);
    }

    /// <summary>SR18's violation half — no OCCURS on the entry and none above it — named as the user's bug
    /// (COBOLNET1944), not as a compiler limitation. Edition-independent: SR18 carries no version proviso, so it
    /// fires at all four (below 2002 the COBOLNET0900 introduction gate fires as well — both, not either).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void NoOccursAnywhere_Rejected1944(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 X PIC X(2) VALUE \"AB\" FROM (1).", "    DISPLAY \"X\"."), edition);
        Assert.False(ok, "a Format 2 VALUE on an entry with no OCCURS above it violates §13.18.63.3 SR18");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1944");
        if (edition == 85) EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    /// <summary>SR20 sentence 1 / SR21 sentence 1 — one subscript per OCCURS clause for the subject or
    /// superordinate to it. Both directions of the mismatch: too many on a one-dimension table, too few on a
    /// two-dimension one, and a TO phrase whose count disagrees with its own FROM.</summary>
    [Theory]
    [InlineData("01 T PIC X(2) OCCURS 3 VALUE \"AB\" FROM (1 1).")]
    [InlineData("01 G OCCURS 2.\n   05 T PIC X(2) OCCURS 3 VALUE \"AB\" FROM (1).")]
    [InlineData("01 G OCCURS 2.\n   05 T PIC X(2) OCCURS 3 VALUE \"AB\" FROM (1 1) TO (2).")]
    public void SubscriptCountMismatch_Rejected1945(string ws)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(ws, "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1945");
    }

    /// <summary>SR21 sentence 3 over a TUPLE: "the table element associated with subscript-2 is the same
    /// occurrence or a successive occurrence of the table element associated with the corresponding subscript-1".
    /// (2 1) IS successive to (1 3) in a 2×3 table — GR12's fill order carries — so it must COMPILE; (1 1) after
    /// (2 1) precedes it and must not.</summary>
    [Fact]
    public void MultiDimension_SuccessiveIsOdometerOrder_NotPerDimension()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 R OCCURS 2.\n      05 C PIC X OCCURS 3 VALUES ARE \"A\" \"B\" FROM (1 3) TO (2 1).",
                 "    DISPLAY \"[\" C(1 3) C(2 1) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AB]", stdout);

        var (bad, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 R OCCURS 2.\n      05 C PIC X OCCURS 3 VALUE \"A\" FROM (2 1) TO (1 1).",
                 "    DISPLAY \"X\"."), 2023);
        Assert.False(bad);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1587");
    }

    /// <summary>SR22's SUBORDINATE arm — "or in any entry subordinate to such an OCCURS clause". The same-entry
    /// arm was written down; this half exited through the staged refusal, so the program was rejected for the
    /// wrong reason.</summary>
    [Fact]
    public void SubordinateToDynamicWithoutTo_Rejected1588()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 G OCCURS DYNAMIC CAPACITY IN C1.\n   05 X PIC X(2) VALUE \"AB\" FROM (1).", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1588");
    }

    /// <summary>SR23 — with a TO phrase over a dimension whose OCCURS DYNAMIC clause has no TO, the subscripts for
    /// every MORE INCLUSIVE level shall be equal (the odometer may not carry out of a dimension with no ceiling).
    /// The equal-higher-subscript twin compiles and seeds.</summary>
    [Fact]
    public void DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946()
    {
        var (bad, diag) = EditionHarness.Compile(
            Prog("01 G OCCURS 2.\n   05 T PIC X OCCURS DYNAMIC CAPACITY IN C1 VALUE \"A\" FROM (1 1) TO (2 3).",
                 "    DISPLAY \"X\"."), 2023);
        Assert.False(bad);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1946");

        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G OCCURS 2.\n   05 T PIC X OCCURS DYNAMIC CAPACITY IN C1 VALUE \"A\" FROM (1 1) TO (1 3).",
                 "    DISPLAY \"[\" T(1 1) T(1 2) T(1 3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AAA]", stdout);
    }

    /// <summary>A GROUP entry's Format 2 VALUE is a GROUP-LEVEL VALUE per occurrence: §13.18.63.3 SR16 carries
    /// SR13 onto it and §13.18.63.4 GR5 initializes "the group area … without consideration for the individual
    /// elementary or group items contained within this group", so "ABCD" lands positionally as P="AB", Q="CD" in
    /// BOTH occurrences. It used to be discarded silently at every edition — the group arm read only the Format-1
    /// carrier (kb/Work PB505).</summary>
    [Fact]
    public void GroupLevelTableValue_InitializesTheAreaPerOccurrence()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 R.\n   05 GT OCCURS 2 VALUE \"ABCD\" FROM (1) TO (2).\n"
               + "      10 P PIC X(2).\n      10 Q PIC X(2).",
                 "    DISPLAY \"[\" P(1) \"/\" Q(1) \"|\" P(2) \"/\" Q(2) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AB/CD|AB/CD]", stdout);
    }

    /// <summary>§13.18.63.4 GR16 with the DYNAMIC dimension OUTSIDE the VALUE-carrying entry: the subordinate
    /// item's TO (3) raises G's initial capacity to 3 ("the initial capacity is increased, if necessary, to the
    /// value of the corresponding subscript-2, provided that this value does not lie outside the range defined by
    /// the minimum and expected capacity"), and each of the three occurrences takes its keyed literal.</summary>
    [Fact]
    public void DynamicOuterDimension_SubordinateTableValue_Gr16Capacity()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G OCCURS DYNAMIC CAPACITY IN C1 FROM 1 TO 4.\n"
               + "   05 X PIC X(2) VALUES ARE \"AB\" \"CD\" FROM (1) TO (3).",
                 "    DISPLAY \"[\" C1 \"][\" X(1) \"|\" X(2) \"|\" X(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("][AB|CD|AB]", stdout);
        Assert.Contains("[0000000003]", stdout);
    }

    /// <summary>A TYPE reference assumes the template's description (ISO §13.18.57.4 GR1), and the VALUE clause is
    /// in neither that GR's nor §13.18.49 GR1's exclusion list — in EITHER of its two formats. The clone used to
    /// copy only <c>RawValue</c>, so a template member's table VALUE was dropped silently and every occurrence
    /// came back VALUE-less (kb/Work PB505's sibling sweep).</summary>
    [Fact]
    public void TypeClone_KeepsItsTableValue()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 TT IS TYPEDEF.\n   05 X PIC X(2) OCCURS 3 VALUES ARE \"AB\" \"CD\" FROM (1).\n01 R TYPE TT.",
                 "    DISPLAY \"[\" X OF R (1) \"|\" X OF R (2) \"|\" X OF R (3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AB|CD|AB]", stdout);
    }

    // ── Edition gating ──

    /// <summary>The COBOL-2002 introduction gate (§13.18.63.2 Format 2): a table VALUE is rejected below 2002 with
    /// COBOLNET0900 (VersionConformancePass ParseArm.VisitValueClause).</summary>
    [Fact]
    public void BelowIntroduction_Rejected0900()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X(3) OCCURS 6 VALUES ARE \"AAA\" \"BBB\" FROM (1).", "    DISPLAY \"X\"."), 85);
        Assert.False(ok, "the Format 2 table VALUE must be rejected at COBOL-85 (introduced 2002)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    // ── The ALL-FORMATS literal screen (kb/Work PB208) ──

    private static string Format1(string pic, string literal) => Prog($"01 W {pic} VALUE {literal}.", "    DISPLAY \"X\".");

    private static string Format2(string pic, string literal) =>
        Prog($"01 T-GRP.\n   03 W {pic} OCCURS 2 VALUE {literal} FROM (1) TO (2).", "    DISPLAY \"X\".");

    private static IReadOnlyList<string> Codes(IEnumerable<string> diagnostics) =>
        [.. diagnostics.SelectMany(d => Regex.Matches(d, @"COBOLNET\d{4}").Select(m => m.Value)).Distinct().Order()];

    /// <summary>⛔ THE DRIFT TEST FOR "ONE SCREEN, EVERY FORMAT" (kb/Work PB208). §13.18.63.3 SR2 is an ALL FORMATS
    /// rule — "If the category of the subject of the entry is numeric, all literals in the VALUE clause shall be
    /// numeric and shall be permissible values within the range indicated by the PICTURE clause or the USAGE
    /// clause" — and SR16 carries SRs 10–15 into format 2 as well, so a literal FORMAT 1 rejects, FORMAT 2 shall
    /// reject, with the same verdict. The two lanes used to disagree completely: the format-1 literal went through
    /// DataBinder's screen and BuildTableValueSpecs' per-occurrence literals went straight to the emitter, so
    /// <c>PIC 9(4) COMP VALUE "0012"</c> was COBOLNET1657 while
    /// <c>PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2)</c> compiled CLEAN at strict 2023 and seeded zeros.
    /// <para>This compares the two lanes' VERDICTS rather than asserting one code, which is the point: a rule added
    /// to the funnel (<c>DataBinder.ScreenValueLiteral</c>) reaches both formats by construction, and a future rule
    /// wired into only one of them reds HERE without anyone having to remember to extend a list. Both the strict
    /// and the --permissive axis are compared — the permissive REWRITE (a class-mismatched literal stored as the
    /// number) has to reach the emitter's per-occurrence override exactly as it reaches item.RawValue.</para></summary>
    [Theory]
    [InlineData("PIC 9(4) COMP", "\"0012\"")]   // SR2 class — an alphanumeric literal on a numeric item (byte form)
    [InlineData("PIC 99", "\"7\"")]             // SR2 class — the same on a zoned DISPLAY item
    [InlineData("PIC 99", "12345")]             // SR2 range — not representable without truncating nonzero digits
    [InlineData("PIC 9(4)", "-1")]              // SR3 — a signed literal on an unsigned subject
    [InlineData("PIC XX", "42")]                // SR4 — a numeric literal on an alphanumeric item
    [InlineData("PIC 99", "SPACE")]             // SR2 — a character figurative on a numeric item
    [InlineData("PIC 99", "12")]                // legal on BOTH — the screen must not over-fire
    [InlineData("PIC XX", "\"AB\"")]            // legal on BOTH
    [InlineData("PIC 99", "ZERO")]              // legal on BOTH — the figurative ZERO is numeric
    public void Format1AndFormat2_ScreenTheSameLiteralAlike(string pic, string literal)
    {
        var strict1 = Codes(EditionHarness.GetDiagnostics(Format1(pic, literal), 2023));
        var strict2 = Codes(EditionHarness.GetDiagnostics(Format2(pic, literal), 2023));
        Assert.Equal(strict1, strict2);

        var perm1 = EditionHarness.CompileFull(Format1(pic, literal), 2023, permissive: true);
        var perm2 = EditionHarness.CompileFull(Format2(pic, literal), 2023, permissive: true);
        Assert.Equal(Codes([.. perm1.Errors, .. perm1.Warnings]), Codes([.. perm2.Errors, .. perm2.Warnings]));
        Assert.Equal(perm1.Ok, perm2.Ok);
    }

    /// <summary>⛔ THE SAME DRIFT TEST FOR THE GROUP SUBJECT (kb/Work PB505). A GROUP entry's VALUE has its own
    /// screen — §13.18.63.3 SR13 sentence 1 ("literal-1 shall be of the same category as the group item or shall
    /// be a figurative constant that is permitted in a MOVE statement to a receiving item of that category") plus
    /// SR4/SR5/SR10's group SIZE sentences — and SR16 carries SR13 onto the format 2 (table) VALUE. That arm read
    /// only the format-1 carrier, which was harmless only while a group entry's table VALUE was DISCARDED: once
    /// §13.18.63.4 GR5's area deposit reached it, an UNSCREENED literal reached storage. MEASURED before the fix:
    /// <c>05 GT OCCURS 2 VALUE "ABCDEFG" FROM (1) TO (2).</c> over two <c>PIC X(2)</c> members deposited a
    /// silently truncated "ABCD", and <c>VALUE 42 FROM (1) TO (2)</c> deposited spaces — both rejected on the
    /// format-1 spelling of the identical entry.</summary>
    [Theory]
    [InlineData("\"ABCD\"")]      // legal on both — exactly the group's 4 character positions
    [InlineData("\"ABCDEFG\"")]   // SR4 sentence 3 — longer than the group item
    [InlineData("42")]            // SR13 sentence 1 — a numeric literal on an alphanumeric group item
    [InlineData("SPACES")]        // legal on both — a figurative constant a MOVE would accept
    public void GroupFormat1AndFormat2_ScreenTheSameLiteralAlike(string literal)
    {
        const string members = "\n      10 P PIC X(2).\n      10 Q PIC X(2).";
        var f1 = Codes(EditionHarness.GetDiagnostics(
            Prog($"01 R.\n   05 GT VALUE {literal}.{members}", "    DISPLAY \"X\"."), 2023));
        var f2 = Codes(EditionHarness.GetDiagnostics(
            Prog($"01 R.\n   05 GT OCCURS 2 VALUE {literal} FROM (1) TO (2).{members}", "    DISPLAY \"X\"."), 2023));
        Assert.Equal(f1, f2);
    }

    /// <summary>SR2 is EDITION-INDEPENDENT, so the format-2 screen fires at all four (§13.18.63.3 SR2 carries no
    /// version proviso). At COBOL-85 the COBOLNET0900 introduction gate fires as well — both, not either.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Format2Sr2Screen_FiresAtEveryEdition(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(Format2("PIC 9(4) COMP", "\"0012\""), edition);
        Assert.False(ok, "an alphanumeric literal on a numeric item is a §13.18.63.3 SR2 violation at every edition");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1657");
        if (edition == 85) EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    /// <summary>⛔ THE FORMAT 2 VALUE ON THE CHARACTER-IMAGE STORAGE LANE (kb/Work PB208 half 2), in the fast gate;
    /// the byte-level pin is <c>conformance:2023/pb208_table_value_image_seed</c>. GroupImageCodec.ImageInitOf —
    /// THE seeder for every image-stored backing since the PB164 consolidation — read only <c>item.RawValue</c>,
    /// which is null for a table VALUE, and its <c>StrRepeat(one, Occurs)</c> then seeded every occurrence with the
    /// VALUE-LESS default, DISCARDING the table VALUE. The REDEFINES alias is what puts B on that lane
    /// (§13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one, so this is conforming).
    /// §13.18.63.4 GR12 initializes occurrence 1 to literal-1 and GR13 reuses it for occurrence 2.</summary>
    [Fact]
    public void ImageStoredLeaf_TakesItsTableValue()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G.\n   05 A PIC X(2) VALUE \"AA\".\n"
               + "   05 B PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).\n"
               + "   05 C PIC X(2) VALUE \"CC\".\n01 R REDEFINES G PIC X(8).",
                 "    DISPLAY \"[\" B(1) \"][\" B(2) \"][\" R(7:2) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[0012][0012][CC]", stdout);
    }

    /// <summary>⛔ THE DRIFT TEST FOR "ONE OCCURRENCE MAP, EVERY STORAGE LANE" (kb/Work PB505). The identical
    /// declaration is compiled TWICE — once alone (the typed-native record-struct fields) and once with a
    /// REDEFINES alias over it, which moves the whole group onto its IMAGE lane (a Tier-B backing: characters for
    /// an ordinary group, PACKED BOOLEAN POSITIONS for a bit group) — and the two runs must print the same thing,
    /// and the §13.18.63.4-derived thing.
    /// <para>Comparing the LANES rather than asserting one code is the point. There are THREE — the record-struct
    /// fields, the character image, and the bit carrier — and each has independently dropped the format-2 VALUE:
    /// the image lane in kb/Work PB208, the bit carrier in PB505 (measured `[0000|0000|0000]` where the same
    /// declaration without the alias gave `[1010|0101|1010]`). The shapes below are the ones the old staged
    /// refusal excluded (a subordinate-item VALUE, a two-dimension odometer, a group-level area VALUE) plus that
    /// bit-carrier case, and each lane reaches them through its own recursion, so a future shape wired into only
    /// one of them reds HERE. §13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one,
    /// so both spellings are conforming.</para></summary>
    [Theory]
    // A single-dimension table on its own OCCURS entry — the shape that always worked, as the control.
    [InlineData("01 G.\n   05 T PIC X(2) OCCURS 3 VALUES ARE \"AB\" \"CD\" FROM (1).",
                "T(1) \"|\" T(2) \"|\" T(3)", 6, "[AB|CD|AB]")]
    // SR18's subordinate arm: the VALUE is on X, the OCCURS on T.
    [InlineData("01 G.\n   05 T OCCURS 3.\n      10 X PIC X(2) VALUES ARE \"AB\" \"CD\" FROM (1).",
                "X(1) \"|\" X(2) \"|\" X(3)", 6, "[AB|CD|AB]")]
    // Two dimensions: GR12's odometer, GR13's cyclic reuse.
    [InlineData("01 G.\n   05 R OCCURS 2.\n      10 T PIC X OCCURS 2 VALUES ARE \"A\" \"B\" \"C\" \"D\" FROM (1 1) TO (2 2).",
                "T(1 1) T(1 2) \"|\" T(2 1) T(2 2)", 4, "[AB|CD]")]
    // A GROUP entry's table VALUE — §13.18.63.4 GR5's area, per occurrence.
    [InlineData("01 G.\n   05 GT OCCURS 2 VALUE \"ABCD\" FROM (1) TO (2).\n      10 P PIC X(2).\n      10 Q PIC X(2).",
                "P(1) \"/\" Q(1) \"|\" P(2) \"/\" Q(2)", 8, "[AB/CD|AB/CD]")]
    // The BIT carrier: a USAGE BIT table inside a bit group. 3 × PIC 1(4) = 12 boolean positions, which
    // §8.5.1.6.3 packs into ceil(12/8) = 2 character positions, so the alias is PIC X(2).
    [InlineData("01 G GROUP-USAGE BIT.\n   05 T PIC 1(4) OCCURS 3 VALUES ARE B\"1010\" B\"0101\" FROM (1).",
                "T(1) \"|\" T(2) \"|\" T(3)", 2, "[1010|0101|1010]")]
    public void AllStorageLanes_SeedTheSameOccurrences(string ws, string reads, int width, string expected)
    {
        string body = $"    DISPLAY \"[\" {reads} \"]\".";
        var native = EditionHarness.CompileAndRun(Prog(ws, body), 2023);
        Assert.True(native.Ok, native.Detail);
        Assert.Contains(expected, native.Stdout);

        var imaged = EditionHarness.CompileAndRun(Prog(ws + $"\n01 R REDEFINES G PIC X({width}).", body), 2023);
        Assert.True(imaged.Ok, imaged.Detail);
        Assert.Contains(expected, imaged.Stdout);
    }

    /// <summary>The --permissive REWRITE reaches the emitter's PER-OCCURRENCE override, not just item.RawValue: a
    /// digits-only alphanumeric literal on a numeric item is read AS the numeric literal SR2 asked for (the CCVS
    /// leniency), so the image-stored table seeds the NUMBER — and warns rather than errors.</summary>
    [Fact]
    public void ImageStoredLeaf_PermissiveRewriteReachesEveryOccurrence()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G.\n   05 A PIC X(2) VALUE \"AA\".\n"
               + "   05 B PIC 9(4) COMP OCCURS 2 VALUE \"0012\" FROM (1) TO (2).\n"
               + "   05 C PIC X(2) VALUE \"CC\".\n01 R REDEFINES G PIC X(8).",
                 "    DISPLAY \"[\" B(1) \"][\" B(2) \"][\" R(7:2) \"]\"."), 2023, permissive: true);
        Assert.True(ok, detail);
        Assert.Contains("[0012][0012][CC]", stdout);
    }
}
