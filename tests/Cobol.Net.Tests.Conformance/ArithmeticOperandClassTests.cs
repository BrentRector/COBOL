// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ISO §8.8.1.1 — THE CLASS OF AN ARITHMETIC OPERAND (DA6). "An arithmetic expression may be an identifier
/// referencing a <b>numeric data item</b>, a numeric literal, the figurative constant ZERO (ZEROS, ZEROES) …".
/// A group item (class alphanumeric, §8.5), an elementary alphanumeric or national item, and a reference-modified
/// slice (§8.4.3.3.4) are therefore NOT permissible arithmetic operands.
/// <para>
/// WiseOwl COBOL accepted all of them and decoded their digit characters — and did so INCONSISTENTLY: a group of
/// <c>PIC X</c> leaves computed, while a group of <c>PIC 9</c> leaves compiled and then THREW at run time. The
/// operand whose digits were unambiguous failed and the merely-textual one succeeded. Owner decision 2026-07-29:
/// reject under strict conformance, keep the leniency DIALECT-GATED behind <c>--permissive</c>.
/// </para>
/// <para>
/// The NEGATIVE half (strict rejection at all four editions) lives in the negative corpus as
/// <c>da6-group-numeric-operand</c> / <c>da6-alphanumeric-numeric-operand</c> / <c>da6-refmod-numeric-operand</c>.
/// THIS file owns the two facts a reject-only fixture cannot express: that the extension still WORKS under
/// <c>--permissive</c>, and that it is CONSISTENT there — which is the actual defect being repaired.
/// </para>
/// </summary>
public sealed class ArithmeticOperandClassTests
{
    private static string Program(string ws, string body) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARITHOPCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
{ws}
       PROCEDURE DIVISION.
       MAIN.
{body}
           STOP RUN.
""";

    /// <summary>⛔ THE FACT THE WHOLE FIX EXISTS FOR. Two groups holding the same digits — one with an alphanumeric
    /// leaf, one with a numeric-DISPLAY leaf — must behave IDENTICALLY. Before DA6 the first computed 001235 and the
    /// second threw <c>NotImplementedCobolFeatureException</c> at run time, so the leniency was not a leniency: it
    /// was a coin toss decided by the leaf's USAGE. Under <c>--permissive</c> both now decode their digits.</summary>
    [Fact]
    public void Permissive_BothGroupKinds_DecodeIdentically()
    {
        const string body = "           COMPUTE R = G + 1.\n           DISPLAY \"R=\" R.";
        var (okAlnum, outAlnum, dAlnum) = EditionHarness.CompileAndRun(
            Program("       01 G.\n          05 A PIC X(4) VALUE \"1234\".\n       01 R PIC 9(6).", body),
            2023, permissive: true);
        var (okNum, outNum, dNum) = EditionHarness.CompileAndRun(
            Program("       01 G.\n          05 A PIC 9(2) VALUE 12.\n          05 B PIC 9(2) VALUE 34.\n       01 R PIC 9(6).", body),
            2023, permissive: true);

        Assert.True(okAlnum, dAlnum);
        Assert.True(okNum, dNum);
        Assert.Equal("R=001235", outAlnum.Trim());
        Assert.Equal(outAlnum.Trim(), outNum.Trim());   // the consistency that was missing
    }

    /// <summary>Every alphanumeric shape §8.8.1.1 bars is rejected under STRICT conformance, and the rule is
    /// EDITION-INVARIANT — §8.8.1.1 is unchanged at 85/2002/2014/2023, so there is no introduction axis and no
    /// gate. Rejecting only the group shape would have left the elementary and reference-modified shapes accepted,
    /// which is the same inconsistency in a new place.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Strict_RejectsEveryAlphanumericOperandShape(int edition)
    {
        foreach (string stmt in new[] { "COMPUTE R = G + 1.", "COMPUTE R = X + 1.", "COMPUTE R = X(1:2) + 1." })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program(
                "       01 G.\n          05 A PIC X(4) VALUE \"1234\".\n"
                + "       01 X PIC X(4) VALUE \"0012\".\n       01 R PIC 9(6).",
                "           " + stmt), edition);
            Assert.False(ok, $"[std {edition}] '{stmt}' must be rejected — ISO §8.8.1.1");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
        }
    }

    /// <summary>⛔ THE FALSE-POSITIVE GUARD, and the reason this rule cannot live at the expression leaf. The
    /// grammar production is named <c>arithmeticExpression</c> but is REUSED as the generic argument expression, so
    /// an intrinsic argument travels the same spine. An argument's legality comes from the FUNCTION's own §15.x
    /// argument rule, which for the string functions admits alphanumeric data — §8.8.1.1 does not govern it.
    /// <para>A first attempt enforced the rule unconditionally inside the leaf and produced 79 conformance failures,
    /// every one of them a legal call like these. These stay legal under STRICT conformance, with no
    /// <c>--permissive</c> needed.</para></summary>
    [Theory]
    [InlineData("FUNCTION LENGTH(S)", "5")]
    [InlineData("FUNCTION ORD(C)", "66")]
    [InlineData("FUNCTION NUMVAL(N)", "12")]
    public void Strict_StillAcceptsAlphanumericIntrinsicArguments(string call, string expectedPrefix)
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 S PIC X(5) VALUE \"HELLO\".\n       01 C PIC X VALUE \"A\".\n"
            + "       01 N PIC X(2) VALUE \"12\".\n       01 R PIC 9(6).",
            $"           COMPUTE R = {call}.\n           DISPLAY \"R=\" R."), 2023);
        Assert.True(ok, $"'{call}' is a legal §15.x alphanumeric ARGUMENT and must compile under strict: {detail}");
        Assert.Contains(expectedPrefix, output);
    }

    /// <summary>⛔ THE LENIENCY STAYS DIALECT-GATED AT THE POSITION OPERANDS TOO (kb/Work PB170;
    /// dialect_two_axes — every leniency is dialect-gated). The new screen sits in
    /// <c>ReferenceResolver.ResolveSubscriptName</c>, on the fast path EVERY subscript and reference-modification
    /// in the corpus takes, and it must have the SAME two lanes the expression-binder screen has: strict rejects,
    /// <c>--permissive</c> warns and decodes the digit characters. The decode is not new behaviour to write —
    /// <c>CobolTable.Occ(string)</c> and <c>CobolString.RefModPosition(string, …)</c> already implement it — so
    /// the permissive lane's EMITTED TEXT is unchanged and only the diagnostic differs.
    /// <para>The strict half is pinned at all four editions by the corpus fixtures
    /// <c>pb170-subscript-alphanumeric-simple</c> / <c>-compound</c> / <c>-edited</c> /
    /// <c>pb170-refmod-alphanumeric-bound</c>; this owns the fact a reject-only fixture cannot express.</para>
    /// </summary>
    [Fact]
    public void Permissive_AlphanumericSubscriptAndRefModBound_StillDecode()
    {
        const string ws = "       01 XE PIC X(4) VALUE \"0002\".\n"
            + "       01 W PIC X(5) VALUE \"ABCDE\".\n       01 R PIC X.\n       01 R2 PIC X(2).\n"
            + "       01 T.\n          05 E PIC X OCCURS 3 TIMES.";
        const string body = "           MOVE \"ABC\" TO T.\n"
            + "           MOVE E(XE) TO R.\n           DISPLAY \"SUB=\" R.\n"
            + "           MOVE W(XE:2) TO R2.\n           DISPLAY \"REFMOD=\" R2.";
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(ws, body), 2023, permissive: true);
        Assert.True(ok, $"--permissive must ACCEPT the digit-decoding extension: {detail}");
        // "0002" decodes to 2: occurrence 2 of "ABC" is 'B', and W(2:2) is "BC".
        Assert.Equal("SUB=B\nREFMOD=BC", output.Trim().Replace("\r\n", "\n"));
    }

    /// <summary>⛔ THE GROUP CARRIER AT A POSITION OPERAND (kb/Work PB201) — the shape the sibling test
    /// above could not reach, because a group is not a <c>string</c> field. The <c>--permissive</c> message
    /// promises to decode "its digit characters as an unsigned integer"; the emitted C# handed
    /// <c>CobolTable.Occ</c> the group's per-program <c>record struct</c> instead (backend CS1503) and, for a
    /// class-tier BASED group, a bare COBOL word that is not a C# name at all (CS0103) — so the promise was
    /// never kept and the two shapes DIVERGED, which is the half of the defect a reject-only fixture cannot
    /// express. They agree now because neither is rendered by the fast path any more: both route to D18, where
    /// <c>NumericRenderer.FieldNum</c> reads the group's §8.5.2.1 alphanumeric IMAGE through the ONE sending
    /// reader.
    /// <para>Expected values, computed from the images: <c>WG</c> is "02" → occurrence 2 of "ABCD" = 'B';
    /// the BASED <c>BG</c> is the same "02" → 'B' (the convergence); the occurs-depending <c>OG</c> sends
    /// only its §13.18.38.4 GR8 CURRENT-count part, "00" + one occurrence "3" = "003" → occurrence 3 =
    /// 'C' (the MAXIMUM image would be "0030 0" and a different number, so this line also pins GR8 reaching the
    /// subscript path); and <c>W(WG:2)</c> is "ABCDE"(2:2) = "BC".</para>
    /// <para>The STRICT half is pinned at every edition by <c>pb201-subscript-group-item</c>,
    /// <c>pb201-refmod-group-bound</c> and <c>pb201-subscript-based-group</c>.</para></summary>
    [Fact]
    public void Permissive_GroupPositionOperands_DecodeTheirImageAndAgree()
    {
        const string ws = "       01 NN PIC 9 VALUE 1.\n"
            + "       01 WG.\n          05 WF1 PIC 9(2) VALUE 2.\n"
            + "       01 BG BASED.\n          05 BF1 PIC 9(2).\n"
            + "       01 OG BASED.\n          05 OF1 PIC 9(2).\n"
            + "          05 OT PIC 9 OCCURS 1 TO 3 TIMES DEPENDING ON NN.\n"
            + "       01 W PIC X(5) VALUE \"ABCDE\".\n"
            + "       01 R PIC X.\n       01 R2 PIC X(2).\n"
            + "       01 T.\n          05 E PIC X OCCURS 4 TIMES.";
        const string body = "           MOVE \"ABCD\" TO T.\n"
            + "           ALLOCATE BG.\n           MOVE 2 TO BF1.\n"
            + "           ALLOCATE OG.\n           MOVE 0 TO OF1.\n           MOVE 3 TO OT(1).\n"
            + "           MOVE E(WG) TO R.\n           DISPLAY \"WS=\" R.\n"
            + "           MOVE E(BG) TO R.\n           DISPLAY \"BASED=\" R.\n"
            + "           MOVE E(OG) TO R.\n           DISPLAY \"ODO=\" R.\n"
            + "           MOVE W(WG:2) TO R2.\n           DISPLAY \"REFMOD=\" R2.";
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(ws, body), 2023, permissive: true);
        Assert.True(ok, $"--permissive must ACCEPT *and compile* a group position operand: {detail}");
        string got = output.Trim().Replace("\r\n", "\n");
        Assert.Equal("WS=B\nBASED=B\nODO=C\nREFMOD=BC", got);
        // The convergence the note demands in its own words: "the two shapes must not diverge".
        Assert.Equal(got.Split('\n')[0]["WS=".Length..], got.Split('\n')[1]["BASED=".Length..]);
    }

    /// <summary>The index DATA item, the arm the private category switch never had (kb/Work PB170). §8.5.2.1
    /// Table 2 puts it in class INDEX and §13.18.60.3 SR10's closed reference list — "a SEARCH or SET statement,
    /// a relation condition, an intrinsic function argument" — has no arithmetic-operand and no subscript entry,
    /// so BOTH shapes below are illegal. Measured on 9a89fbd1: both compiled clean under STRICT, and
    /// <c>COMPUTE N = IDX + 1</c> returned the occurrence number. The receiving-side twin
    /// <c>ScreenResultant</c> already rejected an index item by name — two arms of one rule, one written.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Strict_RejectsAnIndexDataItemAsAPositionOrArithmeticOperand(int edition)
    {
        const string ws = "       01 IDX USAGE INDEX.\n       01 N PIC 9(4).\n       01 R PIC X.\n"
            + "       01 T.\n          05 E PIC X OCCURS 3 TIMES.";
        foreach (string stmt in new[] { "COMPUTE N = IDX + 1.", "MOVE E(IDX) TO R." })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program(ws, "           SET IDX TO 2.\n           " + stmt), edition);
            Assert.False(ok, $"[std {edition}] '{stmt}' must be rejected — ISO §13.18.60.3 SR10 / §8.8.1.1");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
        }
    }

    /// <summary>A genuinely NUMERIC operand is untouched by any of this — the control that proves the screen is not
    /// simply rejecting everything.</summary>
    [Fact]
    public void Strict_AcceptsNumericOperands()
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 N PIC 9(4) VALUE 12.\n       01 M PIC S9(3)V9 VALUE 7.\n       01 R PIC 9(6).",
            "           COMPUTE R = N + M + 1.\n           DISPLAY \"R=\" R."), 2023);
        Assert.True(ok, detail);
        Assert.Equal("R=000020", output.Trim());   // 12 + 7 + 1 — both operands are class NUMERIC
    }

    /// <summary>
    /// ⛔ A NUMERIC-EDITED OPERAND IS REJECTED (owner decision 2026-08-02, REVERSING DA6's admission — the
    /// control above used to be <c>01 E PIC ZZ9 VALUE 7</c> and asserted it de-edited).
    /// <para>
    /// The old reading took de-editing to make a numeric-edited item a legal arithmetic operand. It does not:
    /// §8.8.1.1 admits "an identifier referencing a <b>numeric data item</b>", §8.5.2.13 calls this a
    /// "<b>numeric-edited</b> data item" — a distinct defined term — and §8.5.2.1 Table 2 puts that category in
    /// class ALPHANUMERIC (usage display) or NATIONAL, never numeric. Every de-editing rule in the standard is a
    /// MOVE/editing rule (§14.9.25.4 GR6d1), and GR6d1 has to GRANT de-editing for the MOVE, which would be
    /// unnecessary if it were generally available. The sibling `IntrinsicArgumentRules` 'n' screen had already
    /// refuted this same reading, so the two rested on readings of §8.8.1.1 that could not both be right.
    /// </para>
    /// <para>⚠ Both external oracles were consulted before landing: NO NIST program depends on it (the whole
    /// NIST corpus stayed green through the flip), and GnuCOBOL's own suite exercises de-editing only under
    /// MOVE — every one of its cases is titled "MOVE with de-editting to …".</para>
    /// </summary>
    [Fact]
    public void Strict_RejectsNumericEditedOperand()
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 N PIC 9(4) VALUE 12.\n       01 E PIC ZZ9 VALUE 7.\n       01 R PIC 9(6).",
            "           COMPUTE R = N + E + 1.\n           DISPLAY \"R=\" R."), 2023);
        Assert.False(ok, $"a numeric-edited arithmetic operand must be rejected (ISO §8.8.1.1); got: {output}");
        Assert.Contains("COBOLNET0844", detail + output);
    }

    /// <summary>⛔ ONE SOURCE DEFECT, ONE DIAGNOSTIC (kb/Work PB220). The token renderer's fast path used to run
    /// its §8.8.1.1 screen INSIDE the token loop, and the loop's exits to the D18 materializer are
    /// ORDER-DEPENDENT: a later token with no case arm — <c>**</c> — abandons the fast path AFTER the earlier
    /// name was screened, and D18 then re-binds the same operand through the expression binder, which screens it
    /// again. Measured before the fix: <b>two</b> COBOLNET0844s for one <c>E(XE ** 2)</c>, in BOTH lanes
    /// (Error+Error under strict, Warning+Warning under --permissive). The screens are now queued and flushed
    /// only when the fast path commits, so an exit discards them — deduplication by control flow, not by a set,
    /// which is what makes the NEXT late exit automatic.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReroutedCompoundSegment_DiagnosesExactlyOnce(bool permissive)
    {
        const string ws = "       01 XE PIC X(4) VALUE \"0001\".\n       01 R PIC X.\n"
            + "       01 T.\n          05 E PIC X OCCURS 3 TIMES.";
        var (ok, errors, warnings) = EditionHarness.CompileFull(
            Program(ws, "           MOVE \"ABC\" TO T.\n           MOVE E(XE ** 2) TO R."), 2023, permissive);
        int hits = errors.Concat(warnings).Count(d => d.Contains("COBOLNET0844", StringComparison.Ordinal));
        Assert.True(hits == 1,
            $"[permissive={permissive}] one non-numeric subscript name must draw ONE COBOLNET0844, not {hits} — "
            + "the fast-path screen and the D18 route are two readings of ONE rule, and the reader must not be "
            + "told twice. Got: " + string.Join(" ;; ", errors.Concat(warnings)));
        Assert.Equal(permissive, ok);   // the leniency still decides acceptance; only the COUNT changed
    }

    /// <summary>⛔ §13.18.38.3 r7 GETS ONE LANE POSTURE PER SLOT KIND, AND BOTH ROUTES TO THE SAME SLOT AGREE
    /// (kb/Work PB219). A reference-modification bound is an ARITHMETIC position (§8.4.3.3.3 SR4), so it takes
    /// R29's disposition: strict rejects with the r7 citation, <c>--permissive</c> warns and computes the
    /// occurrence number. Before this, the token renderer's fast path carried R16's IDENTIFIER-slot posture
    /// (an unconditional Error) while its OWN D18 route carried R29's — so under <c>--permissive</c>
    /// <c>W(IX:2)</c> was a hard error and <c>W(IX / 1:2)</c> warned and ran, the same rule and the same
    /// position, keyed on nothing but whether the renderer could render the bound. Both shapes, both lanes.
    /// <para>The two bounds are equal by construction (<c>IX / 1</c> = <c>IX</c>), so a divergence in the ACCEPTED
    /// output would be visible too, not only in the verdict.</para></summary>
    [Theory]
    [InlineData("W(IX:2)", false)]
    [InlineData("W(IX / 1:2)", false)]
    [InlineData("W(IX:2)", true)]
    [InlineData("W(IX / 1:2)", true)]
    public void R7IndexNameInARefModBound_HasOneDispositionOnBothRoutes(string bound, bool permissive)
    {
        const string ws = "       01 W PIC X(5) VALUE \"ABCDE\".\n       01 R2 PIC X(2).\n"
            + "       01 T.\n          05 E PIC X OCCURS 3 TIMES INDEXED BY IX.";
        string body = "           SET IX TO 2.\n           MOVE " + bound + " TO R2.\n           DISPLAY \"RM=\" R2.";
        var (ok, errors, warnings) = EditionHarness.CompileFull(Program(ws, body), 2023, permissive);
        Assert.True(ok == permissive,
            $"[permissive={permissive}] '{bound}' — r7 admits an index-name only as a subscript, in PERFORM/SEARCH "
            + $"VARYING, in SET, or in a relation condition. Got ok={ok}: {string.Join(" ;; ", errors)}");
        EditionHarness.AssertHasDiagnostic(permissive ? warnings : errors, "COBOLNET1637");
        if (!permissive) return;
        var (runOk, output, detail) = EditionHarness.CompileAndRun(Program(ws, body), 2023, permissive: true);
        Assert.True(runOk, detail);
        Assert.Equal("RM=BC", output.Trim());   // the coercion computes occurrence 2 on BOTH routes
    }

    /// <summary>⛔ A PROBE'S PLACE IS NOT A BOUND PLACE (kb/Work PB221). <c>Refs.Probe</c> is a
    /// type-discriminating sniff and is documented as side-effect-free, which means it applies NO position
    /// screen — so a caller that commits the probe's Place into the bound tree silently loses every screen.
    /// Four did. Measured before the fix on this same tree: <c>CALL "S" USING BY CONTENT E(XE)</c> with
    /// <c>XE PIC X(4)</c> compiled CLEAN under strict while the adjacent <c>BY REFERENCE</c> operand — and the
    /// byte-identical <c>MOVE E(XE)</c> — drew COBOLNET0844. One statement, one rule, two verdicts.</summary>
    [Fact]
    public void ProbedCallByContentOperand_IsScreenedLikeEveryOtherReference()
    {
        const string ws = "       01 XE PIC X(4) VALUE \"0002\".\n       01 R PIC X.\n"
            + "       01 T.\n          05 E PIC X OCCURS 3 TIMES.";
        var (contentOk, contentErrors, _) = EditionHarness.CompileFull(
            Program(ws, "           CALL \"PB221SUB\" USING BY CONTENT E(XE)."), 2023);
        var (moveOk, moveErrors, _) = EditionHarness.CompileFull(
            Program(ws, "           MOVE E(XE) TO R."), 2023);
        Assert.False(moveOk, "control: the MOVE form is rejected");
        Assert.False(contentOk,
            "BY CONTENT commits a Probe's Place; the §8.8.1.1 position screen must still apply to it — "
            + string.Join(" ;; ", contentErrors));
        EditionHarness.AssertHasDiagnostic(contentErrors, "COBOLNET0844");
        EditionHarness.AssertHasDiagnostic(moveErrors, "COBOLNET0844");
    }
}
