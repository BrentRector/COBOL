// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ONE SENDER, MANY RECEIVERS, ONE EVALUATION — the drift net under kb/Work PB394's mechanism.
///
/// <para>ISO §14.9.25.4 GR1 (MOVE) makes the sending value an intermediate result item evaluated "only once,
/// immediately before data is moved to the first of the receiving operands"; §14.9.13.4 GR3 (EVALUATE) assigns
/// each selection subject its value "at the beginning of the execution of the EVALUATE statement"; §14.7.7 GR4
/// gives the arithmetic family its ONE initial evaluation; §14.9.39.4 GR12/GR14/GR16/GR18 store one identified
/// address into each SET receiver "in the order specified". Five clauses, one shape — and before PB394, MOVE and
/// EVALUATE were the two that did not implement it, each re-running the source EXPRESSION at every use.</para>
///
/// <para>A value-varying sender is what MEASURES the cardinality, and <c>FUNCTION RANDOM</c> is the one the
/// standard makes reproducible: §15.75.3 r3 — "If a subsequent reference specifies argument-1, a new sequence of
/// pseudo-random numbers is started" — and §15.75.4 r2 — "For a given seed value on a given implementation, the
/// sequence of pseudo-random numbers will always be the same". So every assertion below compares a RE-SEEDED run
/// against a control and never against a particular pseudo-random value: the tests pin the RULE, not this
/// implementation's sequence.</para>
/// </summary>
public sealed class SendingValueOnceDriftTests
{
    /// <summary>Every verb that takes ONE sending operand and MORE THAN ONE receiving operand, written so that
    /// re-evaluating the sender gives the receivers different values. §15.75.4 r1 puts every RANDOM value in
    /// [0, 1), so two receivers of one evaluation are equal and two evaluations are (with probability 1 for this
    /// generator) not.</summary>
    [Theory]
    // MOVE (§14.9.25.4 GR1) — the half PB394 measured wrong: MoveEmitter re-rendered BoundMove.Source in its
    // per-target loop, so the second receiver took a second RANDOM value.
    [InlineData("SVO01", "MOVE FUNCTION RANDOM TO W-A, W-B")]
    // ADD … GIVING / SUBTRACT … GIVING / MULTIPLY … GIVING (§14.7.7 GR4 + NOTE 3 — the one initial evaluation).
    [InlineData("SVO02", "ADD FUNCTION RANDOM 0 GIVING W-A, W-B")]
    [InlineData("SVO03", "SUBTRACT 0 FROM FUNCTION RANDOM GIVING W-A, W-B")]
    [InlineData("SVO04", "MULTIPLY FUNCTION RANDOM BY 1 GIVING W-A, W-B")]
    // COMPUTE (§14.7.7 GR4) — the arm that already carried the rule, kept here so the family is measured as one.
    [InlineData("SVO05", "COMPUTE W-A, W-B = FUNCTION RANDOM")]
    // DIVIDE … GIVING (§14.9.12.4 GR5 → §14.7.7 GR4).
    [InlineData("SVO06", "DIVIDE FUNCTION RANDOM BY 1 GIVING W-A, W-B")]
    public void OneSenderManyReceivers_EveryReceiverTakesTheSameValue(string pid, string stmt)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-SEED PIC 9V9(6).
            01 W-A PIC 9V9(6).
            01 W-B PIC 9V9(6).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                {stmt}.
                IF W-A = W-B
                    DISPLAY "ONCE"
                ELSE
                    DISPLAY "MANY"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("ONCE", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>§14.9.13.4 GR3 over the EVALUATE lowering, at every edition — the subject must be evaluated
    /// EXACTLY once per statement however many WHEN phrases read it, and a THRU object (GR4 a) 5.: "selection-
    /// subject &gt;= left-part AND selection-subject &lt;= right-part") reads the ONE value twice rather than
    /// evaluating the subject twice. Measured by consumption: the control drives the identical WHEN phrases from
    /// a DATA ITEM holding one value, the test drives them from the function, and both then read the next number
    /// of the same re-seeded sequence. Equal reads ⇒ the statement consumed exactly one.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void EvaluateSubject_IsEvaluatedOncePerStatement_AtEveryEdition(int std)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SVOEV{std}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-SEED PIC 9V9(6).
            01 W-SUBJ PIC 9V9(6).
            01 W-CTRL PIC 9V9(6).
            01 W-TEST PIC 9V9(6).
            01 W-CTRL-BR PIC X(5).
            01 W-TEST-BR PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                MOVE FUNCTION RANDOM TO W-SUBJ.
                EVALUATE W-SUBJ
                    WHEN 0 THRU 0.25
                        MOVE "BAND1" TO W-CTRL-BR
                    WHEN 0.25 THRU 0.5
                        MOVE "BAND2" TO W-CTRL-BR
                    WHEN OTHER
                        MOVE "BAND3" TO W-CTRL-BR
                END-EVALUATE.
                MOVE FUNCTION RANDOM TO W-CTRL.
                MOVE FUNCTION RANDOM(1) TO W-SEED.
                EVALUATE FUNCTION RANDOM
                    WHEN 0 THRU 0.25
                        MOVE "BAND1" TO W-TEST-BR
                    WHEN 0.25 THRU 0.5
                        MOVE "BAND2" TO W-TEST-BR
                    WHEN OTHER
                        MOVE "BAND3" TO W-TEST-BR
                END-EVALUATE.
                MOVE FUNCTION RANDOM TO W-TEST.
                IF W-TEST-BR = W-CTRL-BR
                    DISPLAY "BRANCH=SAME"
                ELSE
                    DISPLAY "BRANCH=DIFFERENT"
                END-IF.
                IF W-TEST = W-CTRL
                    DISPLAY "COUNT=ONE"
                ELSE
                    DISPLAY "COUNT=MANY"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(std).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("BRANCH=SAME\nCOUNT=ONE", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ THE INTERMEDIATE MUST HOLD THE SENDER'S LENGTH, NOT A PADDED APPROXIMATION OF IT.
    /// §14.9.25.4 GR1's equivalence is a RESULT equivalence, so the one-receiver and many-receiver forms of the
    /// same MOVE have to agree — and a JUSTIFIED receiver is what MEASURES that, because §13.18.32.4 GR1 aligns
    /// the sending data at the rightmost character position and space-fills on the LEFT, which makes the sender's
    /// length observable where a left-justified receiver hides it. The sender that can get it wrong is one whose
    /// length is decided at run time: §15.4's temporary for a character-category function is carried by a
    /// dynamic-length item, "treated as a fixed-length data item whose length is the dynamic-length elementary
    /// item's current length" (§8.5.1.10.4). <c>FUNCTION TRIM</c> is the shortest sender with that property.
    /// <para>This test is the net under the residue named on GR-14.9.25.4-1: an earlier revision of kb/Work
    /// PB394 froze an OCCURS DEPENDING group sender at its MAXIMUM extent, and a JUSTIFIED receiver is exactly
    /// where that showed up.</para></summary>
    /// <summary>kb/Work PB1007 — §14.9.25.4 GR1's equivalence is only an equivalence if the intermediate MOVES AS the
    /// sender: `MOVE a TO temp / MOVE temp TO b` must store what `MOVE a TO b` stores, for EVERY receiver category.
    /// The §15.4 temporary of an INTEGER function held the value exactly but moved as its 30-digit implementor
    /// description, so each character receiver of a two-receiver MOVE took leading zeros where the one-receiver MOVE
    /// took the function's literal form (DOC-A.1-92): `MOVE FUNCTION INTEGER(N) TO A B` stored 0000. This compares
    /// the hoisted store with the single-receiver control per function and per receiver category, so a new receiver
    /// category or a new temporary description inherits the check rather than the defect.</summary>
    [Theory]
    [InlineData("SVF01", "FUNCTION INTEGER(W-N)")]            // §15.44 — 3
    [InlineData("SVF02", "FUNCTION INTEGER-PART(-12.5)")]     // §15.49 — a negative integer: GR6 a) drops the sign
    [InlineData("SVF03", "FUNCTION ORD(\"A\")")]               // §15.70 — an integer from a character argument
    [InlineData("SVF04", "FUNCTION MOD(-7 3)")]               // §15.64 — 2
    [InlineData("SVF05", "FUNCTION MAX(3 7)")]                // §15.59 — integer when every argument is (§15.2 item 5)
    public void MaterializedIntegerFunction_MovesAsTheFunctionDoes(string pid, string fn)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-N PIC 9V9 VALUE 3.7.
            01 X1 PIC X(6).
            01 X2 PIC X(6).
            01 X3 PIC X(6).
            01 J1 PIC X(6) JUSTIFIED RIGHT.
            01 J2 PIC X(6) JUSTIFIED RIGHT.
            01 N1 PIC N(6).
            01 N2 PIC N(6).
            01 E1 PIC -(5)9.
            01 E2 PIC -(5)9.
            01 S1 PIC S9(6).
            01 S2 PIC S9(6).
            PROCEDURE DIVISION.
            MAIN.
                MOVE {fn} TO X1.
                MOVE {fn} TO X2, X3.
                MOVE {fn} TO J1.
                MOVE {fn} TO J2, E2.
                MOVE {fn} TO N1.
                MOVE {fn} TO N2, S2.
                MOVE {fn} TO E1.
                MOVE {fn} TO S1.
                IF X1 = X2 AND X1 = X3 AND J1 = J2 AND N1 = N2 AND E1 = E2 AND S1 = S2
                    DISPLAY "SAME"
                ELSE
                    DISPLAY "X[" X1 "|" X2 "|" X3 "] J[" J1 "|" J2 "] N[" N1 "|" N2 "] E[" E1 "|" E2 "] S["
                        S1 "|" S2 "]"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("SAME", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>kb/Work PB1007 — the SAME §15.4 temporary crosses an INVOKE: §14.8.2.3.3 2) d) makes a BY CONTENT
    /// argument for a non-numeric formal conform "as for a MOVE statement", and the argument's value reaches the
    /// formal through the materialized temporary. So the formal must hold what `MOVE FUNCTION f TO x` stores into an
    /// item of the formal's description — per formal category. The INVOKE used to refuse every numeric-typed function
    /// here (COBOLNET0828, the expression arm) because the temporary did not move as the function.</summary>
    [Theory]
    [InlineData("SVI01", "FUNCTION INTEGER(W-N)", "PIC X(6)")]
    [InlineData("SVI02", "FUNCTION INTEGER-PART(-12.5)", "PIC X(6) JUSTIFIED RIGHT")]
    [InlineData("SVI03", "FUNCTION ORD(\"A\")", "PIC N(6)")]
    [InlineData("SVI04", "FUNCTION MOD(-7 3)", "PIC -(5)9")]
    public void InvokeContentIntegerFunction_StoresWhatTheMoveStores(string pid, string fn, string pic)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS {pid}C.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE {pid}C.
            01 W-N PIC 9V9 VALUE 3.7.
            01 R {pic}.
            PROCEDURE DIVISION.
            MAIN.
                INVOKE {pid}C "NEW" RETURNING O.
                MOVE {fn} TO R.
                DISPLAY "[" R "]".
                INVOKE O "M" USING BY CONTENT {fn}.
                STOP RUN.
            END PROGRAM {pid}.
            IDENTIFICATION DIVISION.
            CLASS-ID. {pid}C INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            01 P1 {pic}.
            PROCEDURE DIVISION USING P1.
                DISPLAY "[" P1 "]".
            END METHOD M.
            END OBJECT.
            END CLASS {pid}C.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        var lines = stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal(lines[0], lines[1]);
    }

    [Fact]
    public void RunTimeLengthSender_KeepsItsLength_UnderAJustifiedReceiver()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SVOJUST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-J1 PIC X(8) JUSTIFIED RIGHT.
            01 W-J2 PIC X(8) JUSTIFIED RIGHT.
            01 W-T  PIC X(8).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION TRIM("  hi  ") TO W-J1.
                MOVE FUNCTION TRIM("  hi  ") TO W-J2, W-T.
                IF W-J1 = W-J2
                    DISPLAY "SAME"
                ELSE
                    DISPLAY "DIFFERENT"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("SAME", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ THE INTERMEDIATE MUST BE ABLE TO HOLD WHAT IT IS THERE TO PRESERVE, IN EVERY ALPHABET
    /// (kb/Work PB886). §14.9.25.4 GR1's equivalence is a RESULT equivalence — <c>MOVE a (b) TO temp / MOVE temp
    /// TO b</c> owes what <c>MOVE a (b) TO b</c> owes — so the one-receiver form (no intermediate) and the
    /// two-receiver form (an intermediate) must agree for EVERY usage a reference-modified sender can have. The
    /// capacity is counted in §8.4.3.3.4 GR5 a)'s positions ("If the usage of identifier-1 is bit, positions used
    /// in evaluation are bit positions; otherwise … character positions"), which is what
    /// <c>RefModPlace.PositionCount</c> answers; sizing it by the item's CHARACTER OCCUPANCY instead truncated a
    /// boolean slice to one position (<c>MOVE BE(1:4) TO B1 B2</c> gave <c>1000</c>) and a dynamic-length slice
    /// to one character. A usage added later is a row here rather than the next silent wrong answer.</summary>
    [Theory]
    // usage DISPLAY, category alphanumeric — GR5 a)'s "otherwise" arm, the shape that always worked.
    [InlineData("SVOP01", 2002, "01 W-S PIC X(6) VALUE \"ABCDEF\".", "01 W-R1 PIC X(4).", "01 W-R2 PIC X(4).",
                "W-S(1:4)")]
    // usage NATIONAL — GR1's "national position"; one UTF-16 code unit each, never the two bytes each occupies.
    [InlineData("SVOP02", 2002, "01 W-S PIC N(6) USAGE NATIONAL VALUE N\"ABCDEF\".",
                "01 W-R1 PIC N(4) USAGE NATIONAL.", "01 W-R2 PIC N(4) USAGE NATIONAL.", "W-S(1:4)")]
    // usage BIT — GR5 a)'s BIT-position arm. Eight boolean positions occupy ONE character (§8.5.1.6.3).
    [InlineData("SVOP03", 2002, "01 W-S PIC 1(8) USAGE BIT VALUE B\"10110011\".",
                "01 W-R1 PIC 1(4) USAGE BIT.", "01 W-R2 PIC 1(4) USAGE BIT.", "W-S(1:4)")]
    // DYNAMIC LENGTH (§8.5.1.10) — no static character occupancy at all; §8.5.1.10.4 makes the slice's positions
    // the item's CURRENT length, bounded by the §13.18.19.4 GR2 LIMIT.
    [InlineData("SVOP04", 2014, "01 W-S PIC X DYNAMIC LENGTH LIMIT 20.", "01 W-R1 PIC X(4).", "01 W-R2 PIC X(4).",
                "W-S(1:4)")]
    public void RefModIntermediate_HoldsEveryPositionOfTheSlice(
        string pid, int std, string sender, string recv1, string recv2, string slice)
    {
        // The DYNAMIC LENGTH sender needs its value at run time; every other sender carries a VALUE clause.
        string seed = sender.Contains("DYNAMIC LENGTH") ? "    MOVE \"ABCDEF\" TO W-S." : "";
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {sender}
            {recv1}
            {recv2}
            PROCEDURE DIVISION.
            MAIN.
            {seed}
                MOVE {slice} TO W-R1.
                MOVE {slice} TO W-R2, W-R1.
                IF W-R1 = W-R2
                    DISPLAY "SAME"
                ELSE
                    DISPLAY "DIFFERENT"
                END-IF.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(std).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("SAME", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ §14.9.25.4 GR1's OTHER "only once" — the LENGTH: "The length of the data item referenced by
    /// identifier-1 is evaluated only once, immediately before the data is moved to the first of the receiving
    /// operands", and "the evaluation of the length of identifier-1 or identifier-2 may be affected by the
    /// DEPENDING ON phrase of the OCCURS clause". The DISCRIMINATING shape is the one where data-name-1 is
    /// itself a receiving operand, because only then does the extent CHANGE between the two stores: with N = 3
    /// the sender is §13.18.38.4 GR8 a)'s three positions "125", the first store leaves N = 1 (a group sender
    /// makes this GR4's alphanumeric-to-alphanumeric move into a one-character receiving area), and the second
    /// store still owes three positions. Re-reading the length gives "1" (kb/Work PB394's residue).</summary>
    [Fact]
    public void OdoGroupSender_LengthIsFrozen_WhenTheControlItemIsAReceiver()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SVOODO.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-N PIC 9 VALUE 5.
            01 W-G.
               05 W-E PIC 9 OCCURS 1 TO 5 DEPENDING ON W-N.
            01 W-Z PIC X(5) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN.
                MOVE 1 TO W-E(1).
                MOVE 2 TO W-E(2).
                MOVE 5 TO W-E(3).
                MOVE 4 TO W-E(4).
                MOVE 9 TO W-E(5).
                MOVE 3 TO W-N.
                MOVE W-G TO W-N, W-Z.
                DISPLAY "N=" W-N " Z=[" W-Z "]".
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("N=1 Z=[125  ]", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>⛔ THE LEAF SET <c>SendingValueTemp.Model</c> ANSWERS FOR. Its switch names every
    /// <see cref="BoundOperand"/> leaf and decides, per leaf, whether the §14.9.25.4 GR1 intermediate result item
    /// exists and what its description is — so a leaf added later must be a DECISION, not a fall-through. C#
    /// cannot prove a public abstract record's leaf set closed, so the switch carries a throwing arm and this
    /// test carries the roster: adding a <see cref="BoundOperand"/> fails here, in the test that names the rule,
    /// rather than at run time inside somebody's MOVE (feedback_a_dead_lookup_is_also_unverified).</summary>
    [Fact]
    public void BoundOperandLeafSet_IsTheOneSendingValueTempAnswersFor()
    {
        string[] expected =
        [
            nameof(BoundAddressOperand),    // no intermediate — a pointer value created at the reference (PB1021)
            nameof(BoundAllLiteral),        // no intermediate — §8.3.3.6.4 GR2 sizes it from the RECEIVER
            nameof(BoundBoolOperand),       // no intermediate — the §8.8.2 boolean-expression channel
            nameof(BoundComputedOperand),   // §15.4's temporary elementary data item
            nameof(BoundCurrentRecord),     // the record area's description, cloned
            nameof(BoundFieldOperand),      // the item's description, cloned (§8.4.3.3.4 GR6 when ref-modified)
            nameof(BoundFigurative),        // no intermediate — receiver-sized, and GR1's antecedent excludes it
            nameof(BoundNumericLiteral),    // no intermediate — a literal's value cannot change
            nameof(BoundOperandError),      // no intermediate — already diagnosed
            nameof(BoundPredefinedNull),    // no intermediate — §8.4.3.10.1 a predefined address, no storage (PB1427)
            nameof(BoundStringLiteral),     // no intermediate — a literal's value cannot change
        ];
        var actual = typeof(BoundOperand).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(BoundOperand).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, actual);
    }
}
