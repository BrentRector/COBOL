// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Tests.Shared;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The DYNAMIC LENGTH elementary item (ISO §8.5.1.10 / §13.18.19, COBOL-2014; PHASE-12 wave 2): a variable-length,
/// minimum-length-zero PIC X/N string. The declaration-shape rules whose violation must be a LOUD bind-time
/// rejection, never a silent mis-compile (COBOLNET_DESIGN §1.4): §13.18.19.3 SR1 (PICTURE exactly one N or X →
/// COBOLNET1561), a dynamic-length-structure-name no SPECIAL-NAMES clause declares or whose length field a LIMIT
/// exceeds (§13.18.19.3 SR2/SR4 → COBOLNET2258), the DYNAMIC LENGTH STRUCTURE clause's own refusals (COBOLNET2257), and
/// §13.16.3 SR18 (only level-number/entry-name/PICTURE/USAGE/VALUE permitted → COBOLNET1563). The run behavior
/// (truncate-to-LIMIT, current-length MOVE, FUNCTION LENGTH) is the <c>dynamic_length_*</c> conformance corpus; the
/// edition-gating is the <c>dynamic-length-item-2014</c> version-matrix row — these assert the negative gating and
/// the well-formed positive.
/// </summary>
public sealed class DynamicLengthTests
{
    private static string Prog(string entry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DLG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + entry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>§13.18.19.3 SR1 — the PICTURE character-string shall be exactly one instance of 'N' or 'X'; a
    /// numeric PICTURE is a declaration error (COBOLNET1561), never a mis-typed variable-length item.</summary>
    [Fact]
    public void NumericPicture_Rejected1561()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC 9(3) DYNAMIC LENGTH."), 2014);
        Assert.False(ok, "a DYNAMIC LENGTH item with a numeric PICTURE must be rejected (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    /// <summary>§13.18.19.3 SR1 — the boolean symbol '1' is NOT permitted (unlike ANY LENGTH); a dynamic-length
    /// item is alphanumeric or national only (§13.18.19.4 GR1). COBOLNET1561.</summary>
    [Fact]
    public void BooleanPicture_Rejected1561()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC 1 DYNAMIC LENGTH."), 2014);
        Assert.False(ok, "a DYNAMIC LENGTH item with a boolean PICTURE must be rejected (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    private static string WithStructures(string specialNames, string entry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DLS.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
        """ + "\n" + specialNames + "\n" + """
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + entry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>§13.18.19.3 SR2 — dynamic-length-structure-name-1 "shall correspond to a
    /// dynamic-length-structure-name specified in the DYNAMIC LENGTH STRUCTURE clause in the SPECIAL-NAMES
    /// paragraph" (kb/Work PB829). A declared name compiles; an undeclared one is COBOLNET2258. (It used to be
    /// refused whether declared or not — COBOLNET1562, "not yet supported", retired — because the clause that
    /// declares one had no grammar.)</summary>
    [Fact]
    public void StructureName_Declared_Accepted_Undeclared_Rejected2258()
    {
        var (ok, diag) = EditionHarness.Compile(WithStructures(
            "    DYNAMIC LENGTH STRUCTURE MYSTRUCT IS PREFIXED.", "01 WS-D PIC X DYNAMIC LENGTH MYSTRUCT LIMIT IS 10."), 2014);
        Assert.True(ok, $"a declared dynamic-length-structure-name must be accepted:\n{string.Join("\n", diag)}");
        (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH MYSTRUCT LIMIT IS 10."), 2014);
        Assert.False(ok, "an undeclared dynamic-length-structure-name must be rejected (ISO §13.18.19.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET2258");
    }

    /// <summary>§13.18.19.3 SR4 — a LIMIT phrase may not exceed the maximum length associated with the structure:
    /// SHORT PREFIXED holds 65535 (§12.3.7.4 GR18), so 65535 is accepted and 65536 is COBOLNET2258.</summary>
    [Theory]
    [InlineData("65535", true)]
    [InlineData("65536", false)]
    public void StructureName_LimitAboveTheLengthField_Rejected2258(string limit, bool legal)
    {
        var (ok, diag) = EditionHarness.Compile(WithStructures(
            "    DYNAMIC LENGTH STRUCTURE S16 IS SHORT PREFIXED.", $"01 WS-D PIC X DYNAMIC LENGTH S16 LIMIT IS {limit}."), 2014);
        Assert.True(legal == ok, string.Join("\n", diag));
        if (!legal) EditionHarness.AssertHasDiagnostic(diag, "COBOLNET2258");
    }

    /// <summary>§12.3.7.3 SR32 — the implementor specifies the physical-structure-names, and WiseOwl COBOL specifies
    /// none (docs/CONFORMANCE.md §3 D-DL3): the alternative is refused by name, COBOLNET2257. A second declaration of
    /// one name in the paragraph is COBOLNET2257 too (§8.4.2.1), and a repeated PREFIXED phrase is the
    /// choice-indicator rule (§5.2.6.4, COBOLNET2104).</summary>
    [Theory]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 IS VARSTRING.", "COBOLNET2257")]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 IS PREFIXED\n    DYNAMIC LENGTH STRUCTURE S1 IS DELIMITED.", "COBOLNET2257")]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 IS PREFIXED SIGNED PREFIXED.", "COBOLNET2104")]
    public void StructureClause_Refused(string specialNames, string code)
    {
        var (ok, diag) = EditionHarness.Compile(WithStructures(specialNames, "01 WS-D PIC X DYNAMIC LENGTH S1."), 2014);
        Assert.False(ok, "the DYNAMIC LENGTH STRUCTURE clause must be refused");
        EditionHarness.AssertHasDiagnostic(diag, code);
    }

    /// <summary>The printed §12.3.7.2 format, every spelling (rendered: STRUCTURE and IS un-underlined; PREFIXED
    /// and DELIMITED in choice indicators — one or more, any order).</summary>
    [Theory]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 IS PREFIXED.")]
    [InlineData("    DYNAMIC LENGTH S1 SIGNED PREFIXED.")]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 SIGNED SHORT PREFIXED DELIMITED.")]
    [InlineData("    DYNAMIC LENGTH S1 IS DELIMITED SHORT PREFIXED.")]
    [InlineData("    DYNAMIC LENGTH STRUCTURE S1 IS DELIMITED.")]
    public void StructureClause_EverySpelling_Accepted(string specialNames)
    {
        var (ok, diag) = EditionHarness.Compile(WithStructures(specialNames, "01 WS-D PIC X DYNAMIC LENGTH S1."), 2014);
        Assert.True(ok, $"the §12.3.7.2 spelling must be accepted:\n{string.Join("\n", diag)}");
    }

    /// <summary>§13.16.3 SR18 — with DYNAMIC LENGTH the only other clauses permitted are level-number, entry-name,
    /// PICTURE, USAGE, VALUE; OCCURS is excluded (COBOLNET1563).</summary>
    [Fact]
    public void OccursCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH OCCURS 3 TIMES."), 2014);
        Assert.False(ok, "OCCURS with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.16.3 SR18 — JUSTIFIED is likewise excluded (COBOLNET1563).</summary>
    [Fact]
    public void JustifiedCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH JUSTIFIED RIGHT."), 2014);
        Assert.False(ok, "JUSTIFIED with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.16.3 SR18 — GLOBAL is NOT a permitted co-clause; it is decoded post-build for ordinary items, so
    /// the SR18 guard must capture it explicitly (the P12 adversarial-review fix). COBOLNET1563.</summary>
    [Fact]
    public void GlobalCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 10 IS GLOBAL."), 2014);
        Assert.False(ok, "GLOBAL with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.18.19.3 SR1 — a count of 1 in ANY spelling is still ONE instance: <c>PIC X</c>, <c>X(1)</c>,
    /// <c>X(01)</c>, <c>N(001)</c> all denote one X/N position and must be ACCEPTED (the P12 review fix — the former
    /// raw-string match falsely rejected the explicit-count forms).</summary>
    [Theory]
    [InlineData("01 WS-D PIC X(1) DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X(01) DYNAMIC LENGTH LIMIT IS 10.")]
    [InlineData("01 WS-D PIC N(001) DYNAMIC LENGTH.")]
    public void CountOneForm_Accepted(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.True(ok, $"a count-1 PICTURE must be accepted for DYNAMIC LENGTH:\n{string.Join("\n", diag)}");
    }

    /// <summary>§13.18.19.3 SR1 — a count &gt; 1 (two positions) IS rejected (COBOLNET1561), confirming the count-1
    /// acceptance did not weaken the rule.</summary>
    [Theory]
    [InlineData("01 WS-D PIC XX DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X(2) DYNAMIC LENGTH.")]
    public void CountGreaterThanOne_Rejected1561(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.False(ok, "a multi-position PICTURE must be rejected for DYNAMIC LENGTH (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    /// <summary>A well-formed DYNAMIC LENGTH item (PIC X, LIMIT, VALUE — all §13.16.3 SR18-permitted) compiles clean
    /// at COBOL-2014; the positive facts must NOT trip a shape guard.</summary>
    [Theory]
    [InlineData("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 20.")]
    [InlineData("01 WS-D PIC N DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X DYNAMIC LENGTH VALUE \"SEED\".")]
    public void WellFormed_CompilesAt2014(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.True(ok, $"a well-formed DYNAMIC LENGTH item must compile at 2014:\n{string.Join("\n", diag)}");
    }

    /// <summary>The COBOL-2014 introduction gate (§8.5.1.10 / §13.18.19): DYNAMIC LENGTH is rejected below 2014 with
    /// the edition-band COBOLNET0900 (VersionConformancePass ParseArm.VisitDynamicLengthClause).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    public void BelowIntroduction_Rejected0900(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 20."), edition);
        Assert.False(ok, $"DYNAMIC LENGTH must be rejected at COBOL-{edition} (introduced 2014)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    // ── §8.5.1.10.1 — THE MAXIMUM SIZE, and the implementor maximum it falls back to (kb/Work PB463) ──────

    /// <summary>§13.18.19.4 GR2 — "If the LIMIT phrase is not specified, the maximum number of characters that can
    /// be contained by the subject of the entry is implementor-defined." An item written WITHOUT the phrase is
    /// therefore bounded, and ordinary use is unaffected by the bound: the item compiles and runs clean.</summary>
    [Fact]
    public void NoLimitPhrase_CompilesAndRunsUnderTheImplementorMaximum()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DLMAXRUN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-D PIC X DYNAMIC LENGTH.
            01 WS-N PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDE" TO WS-D
                SET SIZE OF WS-D TO 12
                MOVE FUNCTION LENGTH(WS-D) TO WS-N
                DISPLAY "LEN=" WS-N
                STOP RUN.
            """, 2023);
        Assert.True(ok, $"a DYNAMIC LENGTH item with no LIMIT phrase must compile and run: {detail}");
        Assert.Equal("LEN=0012", stdout.Trim());
    }

    /// <summary>§8.5.1.10.1 — the maximum size is "the smallest of" the LIMIT phrase and the implementor maximum, so
    /// a LIMIT phrase ABOVE the implementor maximum is legal (§13.18.19.3 states no rule bounding integer-1 unless a
    /// dynamic-length-structure-name is also written, SR4) and simply cannot raise the maximum. It is REPORTED
    /// (COBOLNET2027) rather than accepted in silence.
    /// <para>⛔ The old reader was an <c>int.TryParse</c>: such a literal FAILED to parse and left the item at the
    /// <c>-1</c> "no bound at all" sentinel, so writing a LIMIT too large REMOVED the bound instead of lowering it
    /// — the compile-side half of kb/Work PB463.</para></summary>
    [Theory]
    [InlineData("2147483648")]            // 2³¹ — one past int.MaxValue, where the old parse gave up
    [InlineData("4294967306")]            // past 2³²
    [InlineData("99999999999999999999999999999999999999999")]   // past Int128 as well
    public void LimitAboveTheImplementorMaximum_CompilesAndIsReported2027(string limit)
    {
        var (ok, errors, warnings) =
            EditionHarness.CompileFull(Prog($"01 WS-D PIC X DYNAMIC LENGTH LIMIT IS {limit}."), 2014);
        Assert.True(ok, $"a LIMIT above the implementor maximum is legal source (ISO §8.5.1.10.1 takes the "
            + $"smallest of the candidates):\n{string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET2027");
    }

    /// <summary>A LIMIT phrase at or below the implementor maximum is the maximum size and says nothing.</summary>
    [Theory]
    [InlineData("20")]
    [InlineData("1073741791")]            // exactly the implementor maximum
    public void LimitWithinTheImplementorMaximum_IsSilent(string limit)
    {
        var (ok, errors, warnings) =
            EditionHarness.CompileFull(Prog($"01 WS-D PIC X DYNAMIC LENGTH LIMIT IS {limit}."), 2014);
        Assert.True(ok, $"a within-maximum LIMIT must compile clean:\n{string.Join("\n", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET2027");
    }

    /// <summary>⛔ THE DRIFT PIN for Annex A.1 item 62, "Dynamic-length elementary items (maximum length)" — a
    /// REQUIRED and DOCUMENTED implementor-defined element (§4.2.5). The number in <c>docs/CONFORMANCE.md</c> §7 row
    /// <c>DOC-A.1-62</c> IS the number the compiler enforces, in both spellings the row publishes, so the published
    /// determination and <see cref="CobolDynString.MaxLength"/> cannot drift apart. A determination that merely
    /// ASSERTS a value is the failure kb/Work PB463 was: §7 pinned "always allocatable … the branch is unreachable"
    /// for years while nothing in the code bounded the request at all.</summary>
    [Fact]
    public void ImplementorMaximum_IsTheNumberPublishedInConformanceSection7()
    {
        string register = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        string row = register.Split('\n').SingleOrDefault(l => l.StartsWith("| DOC-A.1-62 |", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                "docs/CONFORMANCE.md §7 carries no `| DOC-A.1-62 |` row: Annex A.1 item 62 (dynamic-length "
                + "elementary items, maximum length) is a REQUIRED and DOCUMENTED implementor-defined element.");
        Assert.Contains(CobolDynString.MaxLength.ToString("N0", CultureInfo.InvariantCulture), row, StringComparison.Ordinal);
        Assert.Contains("0x" + CobolDynString.MaxLength.ToString("X", CultureInfo.InvariantCulture), row, StringComparison.Ordinal);
    }
}
