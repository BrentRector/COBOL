// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ <b>THE ZERO-LENGTH-LITERAL SUBSTITUTION — ISO §14.9.25.4 GR2, GR3 and GR1's zero-length-ITEM clause</b>
/// (kb/Work PB425). GR2: "If literal-1 is an alphanumeric or national zero-length literal and the receiving
/// operand is other than a dynamic-length elementary item, literal-1 is treated as if it were the figurative
/// constant SPACE." GR3 says the same for "a boolean zero-length literal" and the figurative constant ZERO.
/// GR1: "If identifier-1 is a zero-length item, it is as if literal-1 were specified as a zero-length literal."
///
/// <para>Only the EXCLUSION half existed before: <c>MOVE "" TO a-dynamic-length-item</c> stored length zero, and
/// for every OTHER receiver the empty literal simply flowed on and was padded by the receiver's own alignment
/// rule. That COINCIDES with the required answer wherever the substituted figurative's own value is a space —
/// which is why a large corpus never contradicted it — and stops coinciding exactly where it is not: a NUMERIC
/// or NUMERIC-EDITED receiver under GR2 (<c>MOVE "" TO PIC 9(3)</c> stored 000 where <c>MOVE SPACE</c> stores
/// the space fill) and an ALPHANUMERIC / NATIONAL one under GR3 (<c>MOVE B"" TO PIC X(3)</c> stored spaces where
/// figurative ZERO stores "000", §8.3.3.6.4 GR4 over §14.9.25.4 Table 17).</para>
///
/// <para><b>Both directions are pinned here, because the fix is only correct if it moves the VALUE without
/// moving the DIAGNOSTIC.</b> §14.9.25.3 SR5/SR6/SR7 are SYNTAX rules over a closed list of figurative constants
/// WRITTEN in the source, and a zero-length literal is not one of them: <c>MOVE "" TO PIC 9(3)</c> must still
/// compile at 2023 STRICT (where <c>MOVE SPACE TO PIC 9(3)</c> is COBOLNET0902), and <c>MOVE B"" TO PIC 9(3)</c>
/// must still be refused on the BOOLEAN row of Table 16 rather than accepted as figurative ZERO.</para>
/// </summary>
public sealed class ZeroLengthLiteralMoveTests
{
    private static string Prog(string pid, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    private static string Run(string src, int edition)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(edition).CompileAndRun(src);
        Assert.True(ok, detail);
        return stdout.Replace("\r\n", "\n").TrimEnd('\n');
    }

    // ── GR2 — an ALPHANUMERIC or NATIONAL zero-length literal is the figurative constant SPACE ───────────────

    /// <summary>GR2 across EVERY receiver category Table 16 admits for an alphanumeric sender — alphabetic,
    /// alphanumeric, alphanumeric-edited, boolean, national, numeric and numeric-edited. The values are the
    /// figurative SPACE's, sized by §8.3.3.6.4 GR2 and then EDITED by §14.9.25.4 GR6 where the receiver edits
    /// (<c>PIC XX/XX</c> takes its own insertion character). A BOOLEAN receiver takes DETERMINATION D-B2's
    /// boolean zero — the standard gives figurative SPACE no boolean character value (§8.3.3.6.4 GR5) while
    /// Table 17 gives it category boolean, and §14.6.8.6 fills a boolean receiver with zeros.</summary>
    [Fact]
    public void Gr2_AlphanumericZeroLengthLiteral_IsTheFigurativeSpace_AtEveryReceiverCategory()
    {
        string src = Prog("ZLM01", """
            01 R-ALPHABETIC PIC A(3) VALUE "ABC".
            01 R-ALNUM      PIC X(3) VALUE "???".
            01 R-ALNUM-ED   PIC XX/XX VALUE "AB/CD".
            01 R-BOOL       PIC 1(4) VALUE B"1111".
            01 R-NAT        PIC N(3) VALUE N"ABC".
            01 R-NUM        PIC 9(3) VALUE 123.
            01 R-NUM-ED     PIC ZZ9.
            """, """
            MOVE 123 TO R-NUM-ED.
            MOVE "" TO R-ALPHABETIC.
            MOVE "" TO R-ALNUM.
            MOVE "" TO R-ALNUM-ED.
            MOVE "" TO R-BOOL.
            MOVE "" TO R-NAT.
            MOVE "" TO R-NUM.
            MOVE "" TO R-NUM-ED.
            DISPLAY "AB=[" R-ALPHABETIC "]".
            DISPLAY "AN=[" R-ALNUM "]".
            DISPLAY "AE=[" R-ALNUM-ED "]".
            DISPLAY "BO=[" R-BOOL "]".
            DISPLAY "NA=[" R-NAT "]".
            DISPLAY "NU=[" R-NUM "]".
            DISPLAY "NE=[" R-NUM-ED "]".
            """);
        Assert.Equal("AB=[   ]\nAN=[   ]\nAE=[  /  ]\nBO=[0000]\nNA=[   ]\nNU=[   ]\nNE=[   ]", Run(src, 2014));
    }

    /// <summary>GR2's NATIONAL arm — <c>N""</c> is the same substitution, at the three receiver categories
    /// Table 16 admits for a national sender (boolean, national, numeric).</summary>
    [Fact]
    public void Gr2_NationalZeroLengthLiteral_IsTheFigurativeSpace()
    {
        string src = Prog("ZLM02", """
            01 R-BOOL PIC 1(4) VALUE B"1111".
            01 R-NAT  PIC N(3) VALUE N"ABC".
            01 R-NUM  PIC 9(3) VALUE 123.
            """, """
            MOVE N"" TO R-BOOL.
            MOVE N"" TO R-NAT.
            MOVE N"" TO R-NUM.
            DISPLAY "BO=[" R-BOOL "]".
            DISPLAY "NA=[" R-NAT "]".
            DISPLAY "NU=[" R-NUM "]".
            """);
        Assert.Equal("BO=[0000]\nNA=[   ]\nNU=[   ]", Run(src, 2014));
    }

    /// <summary>⭐ THE EQUIVALENCE GR2 STATES, measured as one: whatever <c>MOVE SPACE TO x</c> produces,
    /// <c>MOVE "" TO x</c> produces — the two are the SAME statement, so no implementation may distinguish
    /// them. Measured where both spellings are legal (pre-2023; §14.9.25.3 SR5 removed the written figurative
    /// into a numeric receiver at 2023, which is exactly why the equivalence is measured at 2014).</summary>
    [Fact]
    public void Gr2_AndMoveSpace_AreTheSameStatement()
    {
        string src = Prog("ZLM03", """
            01 A-ZL PIC 9(3) VALUE 123.
            01 A-FG PIC 9(3) VALUE 123.
            01 B-ZL PIC ZZ9.
            01 B-FG PIC ZZ9.
            """, """
            MOVE 123 TO B-ZL, B-FG.
            MOVE "" TO A-ZL.
            MOVE SPACE TO A-FG.
            MOVE "" TO B-ZL.
            MOVE SPACE TO B-FG.
            IF A-ZL = A-FG AND B-ZL = B-FG
                DISPLAY "SAME"
            ELSE
                DISPLAY "DIFFERENT [" A-ZL "][" A-FG "][" B-ZL "][" B-FG "]"
            END-IF.
            """);
        Assert.Equal("SAME", Run(src, 2014));
    }

    // ── GR3 — a BOOLEAN zero-length literal is the figurative constant ZERO ──────────────────────────────────

    /// <summary>GR3 across every receiver category Table 16 admits for a boolean sender — alphanumeric,
    /// alphanumeric-edited, boolean and national. The alphanumeric legs are the measurable ones: figurative
    /// ZERO's alphanumeric character value is '0' (§8.3.3.6.4 GR4; Table 17 gives ZERO against an alphanumeric
    /// receiving operand the category alphanumeric), so <c>MOVE B"" TO PIC X(3)</c> is "000", not spaces.</summary>
    [Fact]
    public void Gr3_BooleanZeroLengthLiteral_IsTheFigurativeZero()
    {
        string src = Prog("ZLM04", """
            01 R-ALNUM    PIC X(3) VALUE "???".
            01 R-ALNUM-ED PIC XX/XX VALUE "AB/CD".
            01 R-BOOL     PIC 1(4) VALUE B"1111".
            01 R-NAT      PIC N(3) VALUE N"ABC".
            """, """
            MOVE B"" TO R-ALNUM.
            MOVE B"" TO R-ALNUM-ED.
            MOVE B"" TO R-BOOL.
            MOVE B"" TO R-NAT.
            DISPLAY "AN=[" R-ALNUM "]".
            DISPLAY "AE=[" R-ALNUM-ED "]".
            DISPLAY "BO=[" R-BOOL "]".
            DISPLAY "NA=[" R-NAT "]".
            """);
        Assert.Equal("AN=[000]\nAE=[00/00]\nBO=[0000]\nNA=[000]", Run(src, 2014));
    }

    // ── The EXCLUSION, kept beside the rule ──────────────────────────────────────────────────────────────────

    /// <summary>GR2/GR3's own exclusion — "the receiving operand is other than a dynamic-length elementary
    /// item" — so a DYNAMIC LENGTH receiver keeps the zero-length value and its current length is 0
    /// (§8.5.1.10.4). ⭐ And it is PER RECEIVER, which one statement with both kinds of receiver proves: the
    /// dynamic-length item is emptied and the numeric one takes the substituted SPACE, from one MOVE.</summary>
    [Fact]
    public void DynamicLengthReceiver_IsExcluded_PerReceiver()
    {
        string src = Prog("ZLM05", """
            01 D-DYN PIC X DYNAMIC LENGTH LIMIT 10.
            01 R-NUM PIC 9(3) VALUE 123.
            """, """
            MOVE "PRESET" TO D-DYN.
            MOVE "" TO D-DYN, R-NUM.
            DISPLAY "LEN=" FUNCTION LENGTH(D-DYN).
            DISPLAY "NU=[" R-NUM "]".
            """);
        Assert.Equal("LEN=0\nNU=[   ]", Run(src, 2014));
    }

    // ── GR1's zero-length-ITEM clause — the runtime half of the same substitution ─────────────────────────────

    /// <summary>§14.9.25.4 GR1 — "If identifier-1 is a zero-length item, it is as if literal-1 were specified as
    /// a zero-length literal", which lands in GR2. A DYNAMIC LENGTH sending item at current length zero
    /// (§8.5.4 item 4) therefore stores the figurative SPACE into a numeric receiver, exactly as the written
    /// <c>MOVE ""</c> does; the same item at a non-zero length still converts normally, which is the half that
    /// proves the runtime test is a test and not a constant.</summary>
    [Fact]
    public void Gr1_ZeroLengthSendingItem_TakesTheSameSubstitution()
    {
        string src = Prog("ZLM06", """
            01 D-DYN PIC X DYNAMIC LENGTH LIMIT 10.
            01 R-NUM PIC 9(3) VALUE 999.
            01 R-NED PIC ZZ9.
            """, """
            MOVE "" TO D-DYN.
            MOVE D-DYN TO R-NUM.
            MOVE D-DYN TO R-NED.
            DISPLAY "EMPTY-NU=[" R-NUM "]".
            DISPLAY "EMPTY-NE=[" R-NED "]".
            MOVE "77" TO D-DYN.
            MOVE D-DYN TO R-NUM.
            MOVE D-DYN TO R-NED.
            DISPLAY "FULL-NU=[" R-NUM "]".
            DISPLAY "FULL-NE=[" R-NED "]".
            """);
        Assert.Equal("EMPTY-NU=[   ]\nEMPTY-NE=[   ]\nFULL-NU=[077]\nFULL-NE=[ 77]", Run(src, 2014));
    }

    /// <summary>The same GR1 route through a FUNCTION-IDENTIFIER sender (§8.5.4 item 6 — "an intrinsic
    /// function that returns a zero-length value"). It works because GR1's OTHER sentence is honoured first:
    /// "the … function-identifier is evaluated only once", so <c>MoveBinder</c> freezes the result into the
    /// implementor's intermediate result item and the emitted length test reads THAT, never a second call.</summary>
    [Fact]
    public void Gr1_ZeroLengthFunctionResult_TakesTheSameSubstitution()
    {
        string src = Prog("ZLM07", """
            01 W-BLANK PIC X(4) VALUE "    ".
            01 W-FULL  PIC X(4) VALUE "  12".
            01 R-NUM   PIC 9(3) VALUE 999.
            """, """
            MOVE FUNCTION TRIM(W-BLANK) TO R-NUM.
            DISPLAY "EMPTY=[" R-NUM "]".
            MOVE FUNCTION TRIM(W-FULL) TO R-NUM.
            DISPLAY "FULL=[" R-NUM "]".
            """);
        Assert.Equal("EMPTY=[   ]\nFULL=[012]", Run(src, 2014));
    }

    // ── GR1's antecedent is CONDITIONAL — §8.5.4 decides which senders can satisfy it ────────────────────────

    /// <summary>⛔ A NUMERIC function-identifier sender is NOT a §8.5.4 zero-length item, so GR1 never reaches
    /// it and nothing about it may be frozen or re-described. §8.5.4 item 6 is "an intrinsic function that
    /// returns a zero-length value", and §15.4 puts a numeric returned value in "a temporary elementary data
    /// item" whose characteristics §15.4.1 leaves to the implementor — a number, with at least one digit
    /// position, never length zero.
    /// <para><b>Freezing on the SHAPE alone was a wrong answer, not a wasted temp.</b> The sender was hoisted
    /// into the implementor's numeric intermediate (<c>SendingValueTemp.FunctionValuePic</c>, 21 integer + 9
    /// fraction digits), which is NARROWER than a standard-decimal or floating-point result, so the hoist
    /// re-rounded a value that can never be zero-length: this move stored 0.123456789000000.</para></summary>
    [Fact]
    public void Gr1_NumericFunctionSender_IsNotAZeroLengthItem_AndIsNeverReDescribed()
    {
        string src = Prog("ZLM13", """
            01 R-FRAC PIC 9V9(15).
            """, """
            MOVE FUNCTION NUMVAL("0.123456789012345") TO R-FRAC.
            DISPLAY "FRAC=[" R-FRAC "]".
            """);
        Assert.Equal("FRAC=[0123456789012345]", Run(src, 2014));
    }

    /// <summary>⛔ A reference-modified sender is a §8.5.4 zero-length item only "when that has been permitted
    /// by use of the compiler directive REF-MOD-ZERO-LENGTH" (item 9) — the trailing qualifier IS the rule,
    /// because outside such a region §7.3.23.3 GR1 raises EC-BOUND-REF-MOD instead of producing a zero-length
    /// item, so GR1's antecedent can never be satisfied there. Inside one, GR1 hands the slice to GR2 and the
    /// PIC 9(3) receiver takes the figurative SPACE's fill; the SAME reference modification at a non-zero
    /// length is an ordinary alphanumeric-to-numeric elementary move (§14.9.25.4 GR5 / Table 16), which is what
    /// proves the runtime test is a test.</summary>
    [Fact]
    public void Gr1_ReferenceModifiedSender_IsAZeroLengthItemOnlyWhereTheDirectivePermitsIt()
    {
        string src = Prog("ZLM14", """
            01 W-S    PIC X(4) VALUE "1234".
            01 W-ZERO PIC 9    VALUE 0.
            01 W-TWO  PIC 9    VALUE 2.
            01 R-NUM  PIC 9(3) VALUE 999.
            """, """
            >>REF-MOD-ZERO-LENGTH ON
                MOVE W-S(1:W-ZERO) TO R-NUM.
                DISPLAY "RM-ZL=[" R-NUM "]".
                MOVE W-S(1:W-TWO) TO R-NUM.
                DISPLAY "RM-NZ=[" R-NUM "]".
            >>REF-MOD-ZERO-LENGTH OFF
            """);
        Assert.Equal("RM-ZL=[   ]\nRM-NZ=[012]", Run(src, 2023));
    }

    // ── The DIAGNOSTICS stay the LITERAL's ────────────────────────────────────────────────────────────────────

    /// <summary>⛔ The value is the figurative's; the diagnostics are the literal's. §14.9.25.3 SR5 prohibits
    /// "the move of an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or
    /// ALL symbolic-character) to either a numeric item or a numeric-edited item" — a CLOSED LIST of things
    /// written in the source — and ISO 2023 removed it (Annex E.2 item 1). A zero-length literal is not on that
    /// list, so at 2023 STRICT <c>MOVE "" TO PIC 9(3)</c> compiles and <c>MOVE SPACE TO PIC 9(3)</c> does
    /// not. Gating the substituted sender would have turned a wrong answer into a false rejection.</summary>
    [Fact]
    public void ZeroLengthLiteral_IntoNumeric_IsNotTheSr5Removal_At2023Strict()
    {
        var zl = EditionHarness.Compile(Prog("ZLM08", "01 R-NUM PIC 9(3) VALUE 123.", "MOVE \"\" TO R-NUM."), 2023);
        Assert.True(zl.Ok, "MOVE \"\" TO a numeric item is permitted at 2023 — ISO §14.9.25.3 SR5 names only "
            + "figurative constants written in the source; §14.9.25.4 GR2 substitutes a value: "
            + string.Join("\n", zl.Diagnostics));
        var fig = EditionHarness.Compile(Prog("ZLM09", "01 R-NUM PIC 9(3) VALUE 123.", "MOVE SPACE TO R-NUM."), 2023);
        Assert.False(fig.Ok, "the WRITTEN figurative is still the §14.9.25.3 SR5 removal at 2023 strict");
        EditionHarness.AssertHasDiagnostic(fig.Diagnostics, "COBOLNET0902");
    }

    /// <summary>The Table 16 row read is the WRITTEN literal's category, never the substituted figurative's:
    /// §14.9.25.3 SR10 / Table 16 gives a BOOLEAN sending operand "No" against a numeric and against an
    /// alphabetic receiving operand, and a NATIONAL one "No" against an alphanumeric receiver — refusals that
    /// would all evaporate if the screens saw figurative SPACE / ZERO instead.</summary>
    [Theory]
    [InlineData("ZLM10", "01 R PIC 9(3) VALUE 123.", "MOVE B\"\" TO R.")]
    [InlineData("ZLM11", "01 R PIC A(3) VALUE \"ABC\".", "MOVE B\"\" TO R.")]
    [InlineData("ZLM12", "01 R PIC X(3) VALUE \"???\".", "MOVE N\"\" TO R.")]
    public void ZeroLengthLiteral_KeepsItsOwnTable16Row(string pid, string ws, string proc)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Prog(pid, ws, proc), 2014);
        Assert.False(ok, "the zero-length literal's OWN category decides Table 16 (ISO §14.9.25.3 SR10)");
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOLNET0819");
    }

    // ── GR1 IS ONE STATEMENT WITH GR2/GR3 — for EVERY receiver the rule's set contains (kb/Work PB943) ──────────

    /// <summary>⛔ <b>THE DRIFT TEST FOR THE RECEIVER FILTER.</b> §14.9.25.4 GR1 — "If identifier-1 is a
    /// zero-length item, it is as if literal-1 were specified as a zero-length literal" — makes
    /// <c>MOVE zero-length-item TO R</c> and <c>MOVE zero-length-literal TO R</c> the SAME statement for every
    /// receiving operand GR2/GR3 name: "other than a dynamic-length elementary item". So instead of an expected
    /// value per cell, each row moves a zero-length SENDER and its matching zero-length LITERAL ("" · B"" · N"",
    /// by the sender's category) into twin receivers of EVERY category Table 16 admits for that sender, and the
    /// two images must agree. The filter this replaced admitted only numeric and numeric-edited receivers,
    /// excused by a doc comment asserting the other categories "are the same store"; three kinds of row below
    /// measured that false (an edited receiver, a boolean one, and a bit-group sender's GR3 ZERO into a
    /// character one). A new sender shape or receiver category is one line here, and the test cannot pass by
    /// accident: a receiver the route skips shows the group move's or the empty value's image instead.</summary>
    [Theory]
    // an ALPHANUMERIC group (§8.5.4 item 1; §13.18.29.4 GR3) — Table 16 exempts it, so every category is admitted
    [InlineData("ZLG1", "01 G. 05 GE PIC X OCCURS 0 TO 4 DEPENDING ON N.", "\"\"",
        "A(3)|X(3)|XX/XX|1(4)|1(3) USAGE BIT|N(3)|NN/NN|9(3)|ZZ9")]
    // the same, of NATIONAL leaves — its storage image is bytes, two per national position (D-N1)
    [InlineData("ZLG2", "01 G. 05 GE PIC N USAGE NATIONAL OCCURS 0 TO 4 DEPENDING ON N.", "\"\"",
        "A(3)|X(3)|XX/XX|1(4)|1(3) USAGE BIT|N(3)|NN/NN|9(3)|ZZ9")]
    // a BIT group — an elementary BOOLEAN sender (§13.18.29.4 GR1 b)); GR3's ZERO; Table 16's Boolean row
    [InlineData("ZLG3", "01 G GROUP-USAGE BIT. 05 GE PIC 1 OCCURS 0 TO 8 DEPENDING ON N.", "B\"\"",
        "X(3)|XX/XX|1(4)|1(3) USAGE BIT|N(3)|NN/NN")]
    // a NATIONAL group — an elementary NATIONAL sender (GR2 b)); GR2's SPACE; Table 16's National row
    [InlineData("ZLG4", "01 G GROUP-USAGE NATIONAL. 05 GE PIC N OCCURS 0 TO 4 DEPENDING ON N.", "N\"\"",
        "1(4)|1(3) USAGE BIT|N(3)|NN/NN|9(3)|ZZ9")]
    // a DYNAMIC LENGTH item at length zero (§8.5.4 item 4)
    [InlineData("ZLG5", "01 G PIC X DYNAMIC LENGTH LIMIT 9.", "\"\"",
        "A(3)|X(3)|XX/XX|1(4)|1(3) USAGE BIT|N(3)|NN/NN|9(3)|ZZ9")]
    public void Gr1_ZeroLengthItem_IsTheWrittenLiteral_AtEveryReceiverOfGr2sSet(
        string pid, string sender, string literal, string pictures)
    {
        var pics = pictures.Split('|');
        var ws = new System.Text.StringBuilder("01 N PIC 9 VALUE 0.\n" + sender + "\n");
        var proc = new System.Text.StringBuilder(sender.Contains("DYNAMIC") ? "MOVE \"\" TO G.\n" : "");
        for (int i = 0; i < pics.Length; i++)
        {
            ws.Append($"01 I{i} PIC {pics[i]}.\n01 L{i} PIC {pics[i]}.\n");
            proc.Append($"MOVE G TO I{i}.\nMOVE {literal} TO L{i}.\n")
                .Append($"DISPLAY \"I{i}=[\" I{i} \"]\".\nDISPLAY \"L{i}=[\" L{i} \"]\".\n");
        }
        var lines = Run(Prog(pid, ws.ToString(), proc.ToString()), 2023).Split('\n');
        Assert.Equal(2 * pics.Length, lines.Length);   // the population the verdict rests on
        for (int i = 0; i < pics.Length; i++)
            Assert.True(lines[2 * i][1..] == lines[2 * i + 1][1..],
                $"PIC {pics[i]}: the zero-length item stored {lines[2 * i]} but the zero-length literal GR1 says "
                + $"it is AS IF stored {lines[2 * i + 1]} (ISO §14.9.25.4 GR1 → GR2/GR3)");
    }
}
