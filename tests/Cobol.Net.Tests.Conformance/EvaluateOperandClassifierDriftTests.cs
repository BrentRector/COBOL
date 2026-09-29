// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE EVALUATE OPERAND CLASSIFIER IS SYMMETRIC, AND THIS IS WHAT MEASURES IT.
///
/// <para>ISO §14.9.13.3 Table 15 names the same operand kinds on both axes, and §14.9.13.3 SR6 a)/b) are printed
/// as a mirrored pair — so every shape that can be a selection SUBJECT in a given cell can be the selection
/// OBJECT in that cell's transpose. <c>EvaluateOperandCombinationsDriftTests</c> pins the TABLE against the spec;
/// nothing pinned the CLASSIFIER in front of it, and that is exactly where the defect lived: two independently
/// written per-side classifiers, of which only the OBJECT one recognised a switch-status condition-name. So
/// <c>EVALUATE W-ON WHEN TRUE</c> was refused as "an identifier … TRUE or FALSE" while
/// <c>EVALUATE TRUE WHEN W-ON</c> — the same name, the transposed cell — compiled and ran, and the level-88
/// spelling of the subject worked too, which is what proved it a classifier hole rather than a Table-15 fact
/// (kb/Work PB400).</para>
///
/// <para>Each row below is ONE source shape that §14.9.13.4 GR3 e) gives a truth value, written into BOTH
/// positions of the same program. The assertion is not "it compiles": it is that the two positions AGREE, and
/// that they agree on the value the standard assigns. A classifier arm added to one side and not the other
/// fails here even though Table 15 itself is untouched.</para>
/// </summary>
public sealed class EvaluateOperandClassifierDriftTests
{
    /// <summary>One program that writes <paramref name="expr"/> as a selection SUBJECT (against TRUE/FALSE
    /// objects — Table 15's Condition × TRUE-or-FALSE cell) and then as a selection OBJECT (against a TRUE
    /// subject — the transposed cell), so a per-side classification difference shows up as differing output.</summary>
    private static string Prog(string pid, string env, string data, string setup, string expr) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        {env}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-PAD PIC X VALUE SPACE.
        {data}
        PROCEDURE DIVISION.
        MAIN.
            {setup}
            EVALUATE {expr}
                WHEN TRUE
                    DISPLAY "SUBJ=T"
                WHEN FALSE
                    DISPLAY "SUBJ=F"
            END-EVALUATE.
            EVALUATE TRUE
                WHEN {expr}
                    DISPLAY "OBJ=T"
                WHEN OTHER
                    DISPLAY "OBJ=F"
            END-EVALUATE.
            STOP RUN.
        """;

    private const string SwitchEnv = """
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SWITCH-1 IS SWM-A
                ON STATUS IS SW-ON
                OFF STATUS IS SW-OFF.
        """;

    /// <summary>Every bare-operand shape the standard makes a condition, in both selection positions, in both
    /// polarities. The expected text is derived, never observed: §14.9.13.4 GR3 e) assigns the subject a truth
    /// value and GR4 a) 4. selects the WHEN whose TRUE/FALSE matches it, while GR4 a) 3. does the mirror for a
    /// condition OBJECT under a TRUE subject.</summary>
    [Theory]
    // A level-88 condition-name — §8.8.4.2.7 rule 2 makes the bare name a complete condition. W-F holds 1, so
    // the membership test is true.
    [InlineData("EVC01", "", "01 W-F PIC 9 VALUE 1.\n           88 W-IS-ONE VALUE 1.", "", "W-IS-ONE", "SUBJ=T\nOBJ=T")]
    [InlineData("EVC02", "", "01 W-F PIC 9 VALUE 2.\n           88 W-IS-ONE VALUE 1.", "", "W-IS-ONE", "SUBJ=F\nOBJ=F")]
    // A switch-status condition-name — §8.8.4.6. SET … TO ON makes SW-ON true (§14.9.39.4 GR5: the external
    // switch is modified "such that the truth value resultant from evaluation of a condition-name associated
    // with that switch will reflect an on status"). THIS is the pair PB400 found broken on the subject side.
    [InlineData("EVC03", SwitchEnv, "", "SET SWM-A TO ON.", "SW-ON", "SUBJ=T\nOBJ=T")]
    [InlineData("EVC04", SwitchEnv, "", "SET SWM-A TO ON.", "SW-OFF", "SUBJ=F\nOBJ=F")]
    [InlineData("EVC05", SwitchEnv, "", "SET SWM-A TO OFF.", "SW-ON", "SUBJ=F\nOBJ=F")]
    // A ONE-BOOLEAN-CHARACTER boolean item — SR6 b) on the subject side, SR6 a) on the object side. The two
    // arms of one printed rule; only a) existed.
    [InlineData("EVC06", "", "01 W-B PIC 1 USAGE BIT VALUE B\"1\".", "", "W-B", "SUBJ=T\nOBJ=T")]
    [InlineData("EVC07", "", "01 W-B PIC 1 USAGE BIT VALUE B\"0\".", "", "W-B", "SUBJ=F\nOBJ=F")]
    // A one-boolean-character boolean LITERAL — the same SR6 pair over §8.8.2's "a boolean literal" operand.
    [InlineData("EVC08", "", "", "", "B\"1\"", "SUBJ=T\nOBJ=T")]
    [InlineData("EVC09", "", "", "", "B\"0\"", "SUBJ=F\nOBJ=F")]
    public void EveryConditionShape_ClassifiesTheSameAsSubjectAndAsObject(
        string pid, string env, string data, string setup, string expr, string expected)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(Prog(pid, env, data, setup, expr));
        Assert.True(ok, detail);
        Assert.Equal(expected.Replace("\r\n", "\n"), stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>Table 15's OTHER 'Y' in the Condition row — Condition × Condition — over all four truth
    /// combinations, so "the truth values match" cannot pass as "always true" or as "the subject alone".
    /// §14.9.13.4 GR4 a) 3. and a) 4. state that analysis in one shared sentence, and GR3 e) is what makes it
    /// meaningful for a condition-1 subject. Every spelling of condition-1 reaches this cell, so a classifier
    /// that recognises a form on one side only fails here as well as in the TRUE/FALSE test above.</summary>
    [Theory]
    [InlineData("EVCC1", 1, 1, "MATCH")]      // true  × true
    [InlineData("EVCC2", 1, 2, "NOMATCH")]    // true  × false
    [InlineData("EVCC3", 2, 1, "NOMATCH")]    // false × true
    [InlineData("EVCC4", 2, 2, "MATCH")]      // false × false
    public void ConditionAgainstCondition_IsTrueIffTheTruthValuesMatch(string pid, int a, int b, string expected)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-A PIC 9 VALUE {a}.
               88 W-A-ON VALUE 1.
            01 W-B PIC 9 VALUE {b}.
               88 W-B-ON VALUE 1.
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE W-A-ON
                    WHEN W-B-ON
                        DISPLAY "MATCH"
                    WHEN OTHER
                        DISPLAY "NOMATCH"
                END-EVALUATE.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>The NEGATIVE half of the same symmetry, and the reason SR6's length test is not decoration: a
    /// boolean expression that does NOT result in one boolean character is boolean-expression-1/-2, and Table
    /// 15's boolean-expression row/column is blank against TRUE or FALSE. Both positions shall be refused, and
    /// refused by the SAME rule — a per-side difference here is the mirror image of the defect above.</summary>
    [Theory]
    [InlineData("EVN01", "EVALUATE W-B2 WHEN TRUE DISPLAY \"X\" END-EVALUATE.")]      // SR6 b) does not apply
    [InlineData("EVN02", "EVALUATE TRUE WHEN W-B2 DISPLAY \"X\" END-EVALUATE.")]      // SR6 a) does not apply
    [InlineData("EVN03", "EVALUATE B\"01\" WHEN TRUE DISPLAY \"X\" END-EVALUATE.")]
    [InlineData("EVN04", "EVALUATE TRUE WHEN B\"01\" DISPLAY \"X\" END-EVALUATE.")]
    public void AMultiPositionBooleanAgainstTrueOrFalse_IsRefusedOnBothSides(string pid, string proc)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-B2 PIC 1(2) USAGE BIT VALUE B"01".
            PROCEDURE DIVISION.
            MAIN.
                {proc}
                STOP RUN.
            """;
        var (ok, _, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.False(ok, "ISO §14.9.13.3 SR6 reclassifies a boolean operand to a condition only when it results "
            + "in ONE boolean character; otherwise it is boolean-expression-1/-2, and Table 15 leaves that cell "
            + "blank against a TRUE-or-FALSE counterpart");
        Assert.Contains("COBOLNET1634", detail, StringComparison.Ordinal);
    }

    /// <summary>§14.9.13.4 GR1 — "If an operand of the EVALUATE statement consists of a single literal, that
    /// operand is treated as a literal, not as an expression" — read together with §8.3.3.3.2 rule 2, which puts
    /// an ADJACENT sign inside the literal. So a signed literal is a LITERAL on both sides of the pair (Table 15
    /// leaves literal × literal blank), while the SEPARATED spelling is a unary operator applied to one and is a
    /// perfectly legal arithmetic-expression operand. The sign is the only thing that differs between the two
    /// rows, on both sides of the pairing.</summary>
    [Theory]
    [InlineData("EVL01", "EVALUATE 5 WHEN 6", false)]
    [InlineData("EVL02", "EVALUATE -5 WHEN 6", false)]
    [InlineData("EVL03", "EVALUATE +5 WHEN 6", false)]
    [InlineData("EVL04", "EVALUATE 5 WHEN -6", false)]
    [InlineData("EVL05", "EVALUATE - 5 WHEN 6", true)]     // separated ⇒ arithmetic-expression × literal = 'Y'
    [InlineData("EVL06", "EVALUATE 5 WHEN - 6", true)]     // separated ⇒ literal × arithmetic-expression = 'Y'
    public void ASignedSoleLiteral_IsALiteralOnBothSides(string pid, string head, bool permitted)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            PROCEDURE DIVISION.
            MAIN.
                {head}
                    DISPLAY "HIT"
                WHEN OTHER
                    DISPLAY "MISS"
                END-EVALUATE.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        if (permitted)
        {
            Assert.True(ok, detail);
            Assert.Equal("MISS", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
        }
        else
        {
            Assert.False(ok, "ISO §14.9.13.3 SR10 Table 15 leaves the literal × literal cell blank");
            Assert.Contains("COBOLNET1634", detail, StringComparison.Ordinal);
        }
    }
}
