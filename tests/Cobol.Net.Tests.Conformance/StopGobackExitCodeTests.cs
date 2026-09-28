// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The STOP RUN / GOBACK termination-STATUS phrase → process exit-code wiring (ISO §14.9.42 STOP statement /
/// §14.9.18 GOBACK statement). §14.9.42.4 GR5 and §14.9.18.4 GR10 pass the status VALUE to the operating system;
/// GR2/GR3 (STOP) and GR7/GR8 (main-program GOBACK) select an error-vs-normal termination indication. On .NET the
/// single observable is the process exit code (<see cref="Runtime.RunUnit.ExitStatus"/> → <c>Environment.ExitCode</c>),
/// so this compiler's documented implementor mapping (Annex A required-behavior items 192/193; docs/CONFORMANCE.md
/// §4.2.16) collapses both into ONE integer: the STATUS value when present, else ERROR ⇒ 1 / NORMAL ⇒ 0.
/// These facts assert the NUMERIC exit code (the manifest golden harness only checks <c>ExitCode == 0</c> as a bool,
/// so the value is unobservable there). The below-edition rejection of the phrase is covered by the version matrix
/// (stop-run-status-2002 / goback-status-2023 → COBOLNET0900).
/// </summary>
public sealed class StopGobackExitCodeTests
{
    private static string Prog(string pid, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-CODE PIC 9(3) VALUE 13.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "ran".
            {proc}
        """;

    /// <summary>
    /// ⛔ A STATUS VALUE PAST THE HOST'S 32-BIT EXIT CODE IS CLAMPED, NEVER WRAPPED (kb/Work PB1178). The value
    /// used to reach <c>Environment.ExitCode</c> through a bare <c>(int)</c> of a <c>long</c> (itself a bare
    /// <c>(long)</c> of the integer landing), so <c>STOP RUN WITH ERROR STATUS 4294967296</c> (2^32) exited 0 — the
    /// operating system reported a NORMAL termination for an ERROR phrase (§14.9.42.4 GR2) because the value wrapped.
    /// docs/CONFORMANCE.md item 192 documents the range: the value saturates to [-2^31, 2^31 - 1], and a POSIX host
    /// then reports its low 8 bits (so 2^31 - 1 exits 255 there). The 9(20) arm is 2^64 + 3, which a 64-bit wrap
    /// made 3.
    /// </summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS 4294967296.", int.MaxValue)]
    [InlineData("STOP RUN WITH ERROR STATUS WS-WIDE.", int.MaxValue)]
    [InlineData("STOP RUN WITH ERROR STATUS -4294967297.", int.MinValue)]
    [InlineData("GOBACK WITH ERROR STATUS 4294967296.", int.MaxValue)]
    public void StatusBeyondTheHostExitCode_IsClampedNeverWrapped(string proc, int clamped)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGCLAMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-WIDE PIC 9(20) VALUE 18446744073709551619.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "ran".
                {proc}
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.True(stdout == "ran", detail);
        Assert.Equal(OperatingSystem.IsWindows() ? clamped : clamped & 0xFF, exit);
    }

    [Theory]
    // STOP RUN — §14.9.42.4 GR5 (value passed) wins over the ERROR/NORMAL indication (GR2/GR3).
    [InlineData("STOP RUN WITH ERROR STATUS 42.", 42)]     // GR5: the value is passed
    [InlineData("STOP RUN WITH ERROR.", 1)]                // GR2: no value → the error indication
    [InlineData("STOP RUN WITH NORMAL STATUS 7.", 7)]      // GR5: the value wins over NORMAL
    [InlineData("STOP RUN WITH ERROR STATUS 0.", 0)]       // GR5: the value wins over ERROR too (DOC-A.1-192; kb/Work PB1178)
    [InlineData("STOP RUN WITH NORMAL.", 0)]               // GR3: normal indication, no value
    [InlineData("STOP RUN WITH ERROR STATUS WS-CODE.", 13)]// GR5: a data-item status value
    [InlineData("STOP RUN.", 0)]                           // no status phrase — the default (regression lock)
    // main-program GOBACK — §14.9.18.4 GR3 ("operates as if executing a STOP statement") / GR10 (value passed).
    [InlineData("GOBACK WITH ERROR STATUS 5.", 5)]
    [InlineData("GOBACK WITH NORMAL STATUS 0.", 0)]
    [InlineData("GOBACK WITH ERROR.", 1)]
    [InlineData("GOBACK.", 0)]                             // no status phrase — the default
    public void TerminationStatus_SetsProcessExitCode(string proc, int expectedExit)
    {
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(Prog("SGEXIT", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>
    /// ⛔ THE NON-NUMERIC STATUS OPERAND — LEGAL COBOL THIS COMPILER USED TO REJECT (kb/Work PB169).
    /// <para>§14.9.42.2 writes the operand <c>{identifier-1 | literal-1}</c>, not
    /// <c>arithmetic-expression-1</c>, so §8.8.1.1 never governed the position — yet both arms were bound
    /// through <c>ExpressionBinder.BindExpr</c>. Measured on 9a89fbd1: <c>STATUS "ABEND"</c> AND
    /// <c>STATUS WS-DISPLAY</c> (PIC X(3)) each drew COBOLNET0844 citing §8.8.1.1, while §14.9.42.3 SR2
    /// explicitly admits "a data item with usage display or usage national" and SR3's conditional ("If literal-1
    /// IS numeric …") presupposes the non-numeric literal. The rejection quoted a rule the programmer had not
    /// broken — the COBOLNET1628 shape.</para>
    /// <para>The VALUES are docs/CONFORMANCE.md item 192's published GR5 determination, implemented rather than
    /// invented: "the integer value of literal-1 / identifier-1 (truncated toward zero) becomes the exit code; a
    /// non-integer display/national operand is interpreted numerically" — i.e. <c>CobolNum.FromAlphanumeric</c>,
    /// where a non-digit position contributes no digit. ⚠ "ABEND" therefore maps to 0, which is
    /// INDISTINGUISHABLE from NORMAL termination; that is the published mapping, and whether to refine it (an
    /// ERROR status with a non-numeric literal yielding 1) is an owner question recorded in kb/Work PB169, not a
    /// silent choice made here.</para>
    /// </summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS \"007\".", 7)]        // SR3's conditional: a non-numeric literal-1
    [InlineData("STOP RUN WITH ERROR STATUS \"ABEND\".", 0)]      // GR5 + item 192: no digit position → 0
    [InlineData("STOP RUN WITH ERROR STATUS WS-DISPLAY.", 7)]     // SR2: "usage display" — the half PB169's note omits
    [InlineData("STOP RUN WITH NORMAL STATUS WS-NAT.", 12)]       // SR2: "usage national"
    [InlineData("STOP RUN WITH ERROR STATUS WS-PACKED.", 9)]      // SR2's first alternative: an INTEGER data item, any usage
    [InlineData("GOBACK WITH ERROR STATUS \"007\".", 7)]          // §14.9.18.3 SR6/SR7 — the shared phrase, the other verb
    [InlineData("GOBACK WITH ERROR STATUS WS-DISPLAY.", 7)]
    public void NonNumericStatusOperand_IsLegalAndInterpretedNumerically(string proc, int expectedExit)
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB169EXIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DISPLAY PIC X(3) VALUE "007".
            01 WS-NAT     PIC N(3) VALUE N"012".
            01 WS-PACKED  PIC 9(3) COMP-3 VALUE 9.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "ran".
                @PROC@
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(src.Replace("@PROC@", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>⛔ THE EMIT FLOOR FOR THE CARRIER WIDENING. <c>TerminationStatus.Value</c> became a
    /// <c>BoundOperand</c>, and the numeric arm must keep producing the SAME generated C# — otherwise every
    /// existing STOP-RUN-STATUS program's emitted text drifts on a fix that was supposed to only WIDEN
    /// acceptance. It holds by construction (<c>Visit(BoundNumericLiteral)</c> and <c>Visit(BoundNumLiteral)</c>
    /// both reduce to <c>EmitText.UnscaledLit</c> for an integer literal; <c>Visit(BoundFieldOperand)</c> and
    /// <c>Visit(BoundNumRef)</c> are the same <c>FieldNum(Place)</c>), and this asserts the observable rather
    /// than the construction: the four shapes that compiled before still produce their exact exit codes.</summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS 42.", 42)]
    [InlineData("STOP RUN WITH NORMAL STATUS 7.", 7)]
    [InlineData("STOP RUN WITH ERROR STATUS WS-CODE.", 13)]
    [InlineData("GOBACK WITH ERROR STATUS 5.", 5)]
    public void NumericStatusArm_IsUnchangedByTheOperandCarrier(string proc, int expectedExit)
    {
        var (exit, stdout, _) = new CobolNetCompiler(2023).CompileAndRunExit(Prog("PB169FLOOR", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>
    /// ⛔ EVERY <c>literal-1</c> FORM THE GENERAL FORMAT ADMITS, RENDERED — NOT REJECTED (kb/Work PB216).
    /// <para>PB169's widening moved the STATUS literal onto the ONE §8.3.3 literal→operand mapping and then
    /// screened only two of the six shapes that mapping produces. The rest reached the emitter as a
    /// <c>NotImplemented.Value&lt;long&gt;</c> — measured on this tree: <c>STATUS SPACE</c>, <c>STATUS ALL "5"</c>,
    /// <c>STATUS B"01"</c> and <c>STATUS B"1" &amp; B"0"</c> each compiled CLEAN and died at RUN TIME.</para>
    /// <para><b>They are conforming source, so the remedy is a rendering.</b> §8.3.3.6.3 SR1 admits a figurative
    /// constant "whenever 'literal' appears in a format", narrowed only (a) where "the literal is restricted to a
    /// numeric literal" — and §14.9.42.3 SR3's conditional "If literal-1 IS numeric" is the proof that it is not —
    /// and (b) where a syntax rule prohibits it, which none of SR2/SR3/SR4 does. §8.3.3.6.4 GR3 NOTE 2 names the
    /// STOP statement BY NAME and gives the figurative a length of ONE character there (GR3 b); GR3 c gives
    /// <c>ALL literal-1</c> the length of literal-1. Those characters then take docs/CONFORMANCE.md item 192's
    /// published GR5 mapping — <c>CobolNum.FromAlphanumeric</c>, a non-digit position contributing no digit — the
    /// SAME decode <c>STATUS "ABEND"</c> already used. Adding a sixth COBOLNET1704 arm instead would have minted
    /// the rejects-legal-source defect PB169 exists to close, one literal form later.</para>
    /// <para>Every expected value below is DERIVED from that published determination before the run: a bare
    /// figurative is one character and none of SPACE / HIGH-VALUE / LOW-VALUE / QUOTE is a digit, so all four
    /// exit 0; <c>ALL "07"</c> is the two characters "07" ⇒ 7; <c>B"01"</c> is the two boolean characters '0','1'
    /// ⇒ 1; the concatenation <c>B"1" &amp; B"0"</c> folds to "10" ⇒ 10 (§8.8.3.3 GR3); <c>X"3037"</c> is the
    /// alphanumeric literal "07" ⇒ 7 (§8.3.3.2 Format 2).</para>
    /// </summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS SPACE.", 0)]            // GR3 b: one character, not a digit
    [InlineData("STOP RUN WITH ERROR STATUS HIGH-VALUE.", 0)]
    [InlineData("STOP RUN WITH ERROR STATUS LOW-VALUE.", 0)]
    [InlineData("STOP RUN WITH ERROR STATUS QUOTE.", 0)]
    [InlineData("STOP RUN WITH ERROR STATUS ZERO.", 0)]             // GR4: the character '0'
    [InlineData("STOP RUN WITH ERROR STATUS ALL ZEROS.", 0)]        // the grammar's own ALL ZERO alternative
    [InlineData("STOP RUN WITH ERROR STATUS ALL \"5\".", 5)]        // GR3 c: the length of literal-1
    [InlineData("STOP RUN WITH ERROR STATUS ALL \"07\".", 7)]
    [InlineData("STOP RUN WITH ERROR STATUS B\"01\".", 1)]          // §8.3.3.4 — a boolean literal's characters
    [InlineData("STOP RUN WITH ERROR STATUS B\"1\" & B\"0\".", 10)] // §8.8.3.3 GR3 — the folded equivalent literal
    [InlineData("STOP RUN WITH ERROR STATUS X\"3037\".", 7)]        // §8.3.3.2 Format 2 — an alphanumeric literal
    [InlineData("GOBACK WITH ERROR STATUS ALL \"5\".", 5)]          // the shared phrase, the other verb
    [InlineData("GOBACK WITH ERROR STATUS SPACE.", 0)]
    public void FigurativeAllAndBooleanStatusLiterals_RenderUnderTheGR5Mapping(string proc, int expectedExit)
    {
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(Prog("PB216LIT", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>
    /// ⛔ THE SR2 IDENTIFIER ALTERNATIVE, THROUGH THE ONE OPERAND-CATEGORY READER (kb/Work PB217). The screen
    /// opened <c>if (p is RefModPlace || p.Item.IsGroup) return false;</c> — <c>Pic</c> guarded by
    /// <c>IsGroup</c>, the exact spelling <c>DataItem.OperandPic</c>'s own doc comment forbids by name — and so
    /// rejected two shapes SR2 admits, with a diagnostic quoting the rule the operand SATISFIES.
    /// <list type="bullet">
    ///   <item>A REFERENCE-MODIFIED display/national operand: §8.4.3.3.3 SR5 permits reference modification
    ///   wherever such an identifier is permitted, and §8.4.3.3.4 GR6 gives the unique data item "the same class,
    ///   category, and usage as that defined for identifier-1" — the three lettered exceptions rewrite class and
    ///   category only, never USAGE. So SR2's second alternative admits the slice.</item>
    ///   <item>A GROUP-USAGE NATIONAL group: §13.18.29.3 SR3 implies USAGE NATIONAL for the subject and
    ///   §13.18.29.4 GR2 b makes it "treated as though it were an elementary data item of usage national … with
    ///   PICTURE N(m)".</item>
    ///   <item>An ALPHANUMERIC group, and a slice of one (kb/Work PB411): §8.5.2.1 — "An alphanumeric group item
    ///   is treated as though it had a usage of display" — so it IS a data item with usage display and SR2's
    ///   second alternative names it. The screen used to ask <c>DataItem.OperandPic</c>, which is null for such a
    ///   group, and read that null as "no usage at all"; the row asserting the rejection lived in the NEGATIVE
    ///   theory below and pinned it (<c>feedback_green_test_can_hold_a_gap_open</c>). The usage answer now comes
    ///   from <c>ItemCategory.UsageOf</c>, the ONE §8.5.2.1 reader.</item>
    /// </list>
    /// Measured on this tree before each fix: every row drew COBOLNET1704. The reader settles all group kinds
    /// with no hand-list — the BIT group stays rejected, and so do the two group kinds §3.11 excludes from
    /// "alphanumeric group item", which is what the negative half below asserts.</summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS WS-DISPLAY(2:2).", 7)]   // "007"(2:2) = "07"
    [InlineData("STOP RUN WITH ERROR STATUS WS-DISPLAY(1:3).", 7)]
    // GR2 redefines a usage-DISPLAY numeric subject as alphanumeric OF THE SAME SIZE for the purposes of the
    // slice, so PIC 9(4) VALUE 1234 has the four character positions '1','2','3','4' and (2:2) is "23" — GR6 c
    // then makes the result category alphanumeric, which SR2's *usage* alternative admits (usage display is
    // preserved by GR6). ⚠ This row was first written as 34, i.e. the slice (3:2); the ordinal is one-based from
    // the LEFTMOST position, and the test caught the arithmetic before the expectation was believed.
    [InlineData("STOP RUN WITH ERROR STATUS WS-NUM9(2:2).", 23)]
    [InlineData("STOP RUN WITH ERROR STATUS WS-NATGRP.", 12)]        // §13.18.29.4 GR2 b
    [InlineData("GOBACK WITH ERROR STATUS WS-DISPLAY(2:2).", 7)]
    // §8.5.2.1's group arm (kb/Work PB411) — the whole group images "41", and BOTH verbs take it: §14.9.42.3 SR2
    // and §14.9.18.3 SR6 are the same rule word for word, and they shared the one screen that rejected it.
    [InlineData("STOP RUN WITH ERROR STATUS WS-ANGRP.", 41)]
    [InlineData("GOBACK WITH ERROR STATUS WS-ANGRP.", 41)]
    // A SLICE of an alphanumeric group: §8.4.3.3.3 SR5 permits reference modification of a class-alphanumeric
    // identifier and §8.4.3.3.4 GR6 keeps identifier-1's usage, which §8.5.2.1 has just settled as display.
    [InlineData("STOP RUN WITH ERROR STATUS WS-ANGRP(1:1).", 4)]
    public void RefModAndGroupStatusOperands_AreAdmittedBySR2(string proc, int expectedExit)
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB217EXIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DISPLAY PIC X(3) VALUE "007".
            01 WS-NUM9    PIC 9(4) VALUE 1234.
            01 WS-NATGRP GROUP-USAGE IS NATIONAL.
               05 WS-NG-A PIC N(3) VALUE N"012".
            01 WS-ANGRP.
               05 WS-AG-A PIC X VALUE "4".
               05 WS-AG-B PIC X VALUE "1".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "ran".
                @PROC@
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(src.Replace("@PROC@", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>The position's OWN syntax rules, which nothing enforced while §8.8.1.1 was standing in for them:
    /// SR4/SR8 (no zero-length literal), SR3/SR7 (a numeric literal-1 shall be an integer), SR2/SR6 (identifier-1
    /// shall be an integer item or a display/national item — which a BIT group is not, and which §8.5.2.1 gives
    /// an ALPHANUMERIC group, so that shape moved to the positive theory above), and §13.18.38.3 r7
    /// (the STATUS phrase is not one of the five contexts admitting an index-name). Each compiled CLEAN on
    /// 9a89fbd1 — the screen that WAS there rejected the legal shapes and admitted the illegal ones, in both
    /// directions at once. (The corpus fixtures pin the same rules at every edition; these assert the MESSAGE
    /// names the rule broken, which a reject-only fixture cannot.)</summary>
    [Theory]
    [InlineData("STOP RUN WITH ERROR STATUS \"\".", "COBOLNET1704", "§14.9.42.3 SR4")]
    [InlineData("STOP RUN WITH ERROR STATUS 1.5.", "COBOLNET1704", "§14.9.42.3 SR3")]
    [InlineData("STOP RUN WITH ERROR STATUS WS-IX.", "COBOLNET1637", "§13.18.38.3 r7")]
    [InlineData("GOBACK WITH ERROR STATUS \"\".", "COBOLNET1704", "§14.9.18.3 SR8")]
    // ⛔ THE SCREEN IS KEYED ON THE BOUND SHAPE, NOT ON THE PARSE ARM (kb/Work PB216). §13.10.4 GR1 makes a
    // constant-name's effect "as if literal-1 … were written where constant-name-1 is written", so SR3 governs a
    // constant-name status operand — which arrives on the dataReference arm and, while SR3 lived inside the
    // literal arm, skipped the rule entirely: measured, `STATUS WS-K` with `01 WS-K CONSTANT AS 1.5` compiled
    // clean and exited 1.
    [InlineData("STOP RUN WITH ERROR STATUS WS-K.", "COBOLNET1704", "§14.9.42.3 SR3 via §13.10.4 GR1")]
    // NULL is not literal-1 (§8.3.3.6.2 lists no NULL figurative format); it is an IDENTIFIER (§8.4.3.1.2 Format 8),
    // so it fills the identifier-1 slot, and the rule it breaks FIRST is the one deciding whether it may be written
    // there at all: §8.4.3.10.3 SR1 confines it to INITIALIZE/SET, a prototype argument or a pointer-or-object-
    // reference relation condition. §14.9.42.3 SR2's category test cannot even be asked — NULL references a
    // data-pointer item only through an associated data item (§8.4.3.10.4 GR1), and a STATUS phrase associates
    // none. So it draws the ONE §8.4.3.10.3 SR1 code (kb/Work PB1427), not the position's own 1704.
    [InlineData("STOP RUN WITH ERROR STATUS NULL.", "COBOLNET2576", "§8.4.3.10.3 SR1")]
    // A BIT group is neither an integer data item nor a display/national one — the arm the ONE §8.5.2.1 usage
    // reader keeps rejecting (usage bit) while it admits the NATIONAL and the ALPHANUMERIC group (PB217/PB411).
    [InlineData("STOP RUN WITH ERROR STATUS WS-BITGRP.", "COBOLNET1704", "§14.9.42.3 SR2")]
    // ⛔ THE TWO GROUP KINDS §3.11 EXCLUDES BY NAME from "alphanumeric group item" — "group item except for a
    // bit group item, a national group item, a strongly-typed group item, or a variable-length group item" — so
    // §8.5.2.1's "treated as though it had a usage of display" does NOT reach them and neither alternative of
    // SR2/SR6 admits them. Without these rows the §8.5.2.1 fix would read as "any group is display", which is
    // the over-correction the rule's own definition forbids (kb/Work PB411; the shape ItemCategory.UsageOf
    // answers null for).
    [InlineData("STOP RUN WITH ERROR STATUS WS-STRONG.", "COBOLNET1704", "§14.9.42.3 SR2 + §3.11")]
    [InlineData("STOP RUN WITH ERROR STATUS WS-VARLEN.", "COBOLNET1704", "§14.9.42.3 SR2 + §3.11")]
    [InlineData("GOBACK WITH ERROR STATUS WS-STRONG.", "COBOLNET1704", "§14.9.18.3 SR6 + §3.11")]
    public void TheStatusPositionsOwnRules_AreEnforcedAndCited(string proc, string code, string clause)
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB169NEG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-K CONSTANT AS 1.5.
            01 WS-TD IS TYPEDEF STRONG.
               05 WS-TD-A PIC X(2).
            01 WS-STRONG TYPE WS-TD.
            01 WS-VARLEN.
               05 WS-VL-A PIC X DYNAMIC LENGTH LIMIT IS 4.
            01 WS-BITGRP GROUP-USAGE IS BIT.
               05 WS-B PIC 1(8) USAGE BIT VALUE B"00000111".
            01 WS-T.
               05 WS-E PIC X OCCURS 3 TIMES INDEXED BY WS-IX.
            PROCEDURE DIVISION.
            MAIN.
                SET WS-IX TO 2.
                @PROC@
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src.Replace("@PROC@", proc), 2023);
        Assert.False(ok, $"'{proc}' must be rejected — {clause}");
        EditionHarness.AssertHasDiagnostic(errors, code);
        // ⛔ AND IT MUST NAME THE RULE THE PROGRAMMER BROKE. Citing §8.8.1.1 here is the defect, not a detail:
        // a diagnostic quoting a rule that does not govern the position sends the reader to the wrong clause.
        Assert.DoesNotContain(errors, e => e.Contains("§8.8.1.1", StringComparison.Ordinal));
    }

    /// <summary>A GOBACK status phrase in a CALLED subprogram is INERT (ISO §14.9.18.4 GR2 returns to the
    /// activator; the STATUS/ERROR indication of GR3/GR7–GR10 applies "in a main program" only). The sub's
    /// <c>GOBACK WITH ERROR STATUS 9</c> must NOT set the exit code — the main resumes after the CALL and its
    /// plain STOP RUN leaves the exit code 0 (the <c>!__asCalled</c> emit guard).</summary>
    [Fact]
    public void CalledSubprogramGobackStatus_IsInert()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGMAIN.
            PROCEDURE DIVISION.
            MAIN.
                CALL "SGSUB".
                DISPLAY "resumed".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSUB.
            PROCEDURE DIVISION.
            SUBMAIN.
                GOBACK WITH ERROR STATUS 9.
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("resumed", stdout);
        Assert.Equal(0, exit);
    }

    /// <summary>The in-group companion to the cross-assembly case below: a status-free MAIN CALLs a sub whose
    /// <c>STOP RUN … WITH STATUS</c> ends the whole run unit (ISO §14.9.42.4 GR6). STOP RUN passes its status
    /// regardless of whether it runs in the main or a called program (unlike GOBACK, GR2/GR3 — see
    /// <see cref="CalledSubprogramGobackStatus_IsInert"/>). Locks that the runtime-side flush (the
    /// <see cref="Runtime.RunUnit.ExitStatus"/> setter) — not a compile-time parse-tree scan — carries the status
    /// to the exit code (§14.9.42.4 GR5).</summary>
    [Fact]
    public void CalledSubprogram_StopRunWithStatus_SetsExitCode()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSMAIN.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "ran".
                CALL "SGSSUB".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSSUB.
            PROCEDURE DIVISION.
            SUBP.
                STOP RUN WITH ERROR STATUS 24.
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("ran", stdout);   // the sub's STOP RUN ends the run unit before the main's own STOP RUN
        Assert.Equal(24, exit);
    }

    // ── V47 (§24 review ledger): STOP RUN … WITH STATUS in a SEPARATELY-COMPILED module crosses the boundary ──

    private static void CompileTo(string source, string dir, string name)
    {
        string src = Path.Combine(dir, name + ".cob");
        src = CompiledProgramCache.StageSource(src, source);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>V47 (§24 review ledger — CONFIRMED): STOP RUN terminates the WHOLE run unit from anywhere (ISO
    /// §14.9.42.4 GR6) and its STATUS is "passed to the operating system" (GR5). When the <c>STOP RUN … WITH
    /// STATUS</c> executes in a SEPARATELY-COMPILED CALLed module, the status is the RUN UNIT's, not the main
    /// program's — it must reach the process exit code even though the main program's own compilation group carries
    /// no status phrase (so a compile-time parse-tree scan of the main group can never see it). The exit-code flush
    /// is runtime-side (the <see cref="Runtime.RunUnit.ExitStatus"/> setter over the shared ambient run unit), so
    /// the sub's status crosses the assembly boundary. Regression lock for the pre-fix silent discard-to-0.</summary>
    [Fact]
    public void SeparatelyCompiledModule_StopRunWithStatus_CrossesAssemblyBoundary()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V47MAIN.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "before".
                CALL "V47SUB".
                DISPLAY "unreached".
                STOP RUN.
            """;
        const string sub = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V47SUB.
            PROCEDURE DIVISION.
            SUBP.
                STOP RUN WITH ERROR STATUS 16.
            """;
        string dir = CutRunner.NewTempDir("v47xasm");
        try
        {
            CompileTo(main, dir, "V47MAIN");
            CompileTo(sub, dir, "V47SUB");
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "V47MAIN.dll"), dir);
            Assert.Equal("before", stdout);   // the sub's STOP RUN ends the run unit — "unreached" never prints
            Assert.Equal(16, exit);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
