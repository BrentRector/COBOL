// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE STATEMENT-SEQUENCE PLACEMENT MATRIX — the drift net under kb/Work PB397's mechanism: the three syntax
/// rules stated over a statement SEQUENCE, each asked of the ONE funnel check (<c>PlacementRules.RefusedOutOfSequence</c>
/// over <c>StatementPosition</c>), with every expected verdict COMPUTED FROM THE RULE TEXT.
///
/// <para>The rules, each <c>cite.py --check</c>ed:
/// <list type="bullet">
///   <item>§14.9.14.3 SR1 — "The EXIT statement shall appear in a sentence by itself that shall be the only sentence
///         in the paragraph or in a section without paragraphs." (FORMAT 1 only: EXIT PERFORM, EXIT PARAGRAPH and the
///         rest are Formats 2-4 and carry no such rule.)</item>
///   <item>§14.9.17.3 SR2 — "If a GO TO statement represented by format 1 appears in a consecutive sequence of
///         imperative statements within a sentence, it shall appear as the last statement in that sequence."
///         (Format 2, DEPENDING, is not constrained.)</item>
///   <item>§14.9.42.3 SR1 — "The STOP statement shall be specified only as the last statement in any discreet block
///         of code." Read as the same consecutive sequence (docs/CONFORMANCE.md D-SEQ); STOP RUN only, since the
///         X3.23-1985 STOP literal continues with the next statement.</item>
/// </list></para>
///
/// <para>Each row varies the POSITION — sentence, phrase of a conditional statement, inline PERFORM body, paragraph
/// sentence count — which is the axis the binder did not carry. The rules name no edition, so every row runs at
/// every edition.</para>
/// </summary>
public sealed class StatementSequencePlacementTests
{
    private const string ExitNotAlone = "COBOLNET2966";
    private const string GoToNotLast = "COBOLNET2967";
    private const string StopNotLast = "COBOLNET2968";

    // ── §14.9.14.3 SR1 ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>A bare EXIT as the whole of a paragraph — the shape the rule admits — alone or among other
    /// paragraphs, and as the only sentence of a section without paragraphs.</summary>
    [Theory]
    [InlineData("SQE01", "MAIN-PARA.\n    PERFORM EX-PARA.\n    STOP RUN.\nEX-PARA.\n    EXIT.\n")]
    [InlineData("SQE02", "MAIN-SECT SECTION.\n    PERFORM EX-SECT.\n    STOP RUN.\nEX-SECT SECTION.\n    EXIT.\n")]
    // Formats 2-4 are not Format 1: each shares its sentence or paragraph and is not governed by SR1.
    [InlineData("SQE03", "MAIN-PARA.\n    DISPLAY \"A\" EXIT PARAGRAPH.\n    DISPLAY \"B\".\n    STOP RUN.\n")]
    [InlineData("SQE04", "MAIN-PARA.\n    PERFORM 2 TIMES DISPLAY \"A\" EXIT PERFORM END-PERFORM.\n    STOP RUN.\n")]
    public void BareExit_InASentenceByItselfAloneInItsParagraph_IsAccepted(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions.Where(e => e >= (pid is "SQE03" or "SQE04" ? 2002 : 85)))
        {
            var (ok, diagnostics) = EditionHarness.Compile(Program(pid, procedure), edition);
            EditionHarness.AssertNoDiagnostic(diagnostics, ExitNotAlone);
            Assert.True(ok, $"{pid} @{edition}: {string.Join(" | ", diagnostics)}");
        }
    }

    [Theory]
    // Shares its sentence with another statement.
    [InlineData("SQE11", "MAIN-PARA.\n    DISPLAY \"A\" EXIT.\n    STOP RUN.\n")]
    // A sentence by itself, but the paragraph has other sentences (before, after).
    [InlineData("SQE12", "MAIN-PARA.\n    DISPLAY \"A\".\n    EXIT.\n    STOP RUN.\n")]
    [InlineData("SQE13", "MAIN-PARA.\n    STOP RUN.\nEX-PARA.\n    EXIT.\n    DISPLAY \"A\".\n")]
    // Written inside another statement: the sentence is the IF, not the EXIT.
    [InlineData("SQE14", "MAIN-PARA.\n    IF W-N = 0 EXIT END-IF.\n    STOP RUN.\n")]
    // A section without paragraphs that has two sentences.
    [InlineData("SQE15", "MAIN-SECT SECTION.\n    PERFORM EX-SECT.\n    STOP RUN.\nEX-SECT SECTION.\n    DISPLAY \"A\".\n    EXIT.\n")]
    public void BareExit_NotAloneInItsParagraph_IsRefused(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions)
            EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Program(pid, procedure), edition), ExitNotAlone);
    }

    // ── §14.9.17.3 SR2 ───────────────────────────────────────────────────────────────────────────────────

    [Theory]
    // Last of its sentence.
    [InlineData("SQG01", "MAIN-PARA.\n    DISPLAY \"A\" GO TO P2.\nP2.\n    STOP RUN.\n")]
    // The whole THEN phrase, with statements written after END-IF: the GO TO ends ITS sequence.
    [InlineData("SQG02", "MAIN-PARA.\n    IF W-N = 0 GO TO P2 END-IF DISPLAY \"A\".\nP2.\n    STOP RUN.\n")]
    [InlineData("SQG03", "MAIN-PARA.\n    IF W-N = 0 GO TO P2 ELSE GO TO P3 END-IF.\nP2.\n    STOP RUN.\nP3.\n    STOP RUN.\n")]
    // Format 2 (DEPENDING): control falls through to the next statement when no name is selected (GR2).
    [InlineData("SQG04", "MAIN-PARA.\n    GO TO P2 P3 DEPENDING ON W-N DISPLAY \"A\".\nP2.\n    STOP RUN.\nP3.\n    STOP RUN.\n")]
    public void Format1GoTo_LastInItsSequence_IsAccepted(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Program(pid, procedure), edition);
            EditionHarness.AssertNoDiagnostic(diagnostics, GoToNotLast);
            Assert.True(ok, $"{pid} @{edition}: {string.Join(" | ", diagnostics)}");
        }
    }

    [Theory]
    [InlineData("SQG11", "MAIN-PARA.\n    GO TO P2 DISPLAY \"A\".\nP2.\n    STOP RUN.\n")]
    // Inside the phrase of a conditional statement: the phrase's own sequence.
    [InlineData("SQG12", "MAIN-PARA.\n    IF W-N = 0 GO TO P2 DISPLAY \"A\" END-IF.\nP2.\n    STOP RUN.\n")]
    [InlineData("SQG13", "MAIN-PARA.\n    IF W-N = 0 DISPLAY \"A\" ELSE GO TO P2 DISPLAY \"B\" END-IF.\nP2.\n    STOP RUN.\n")]
    // Inside an inline PERFORM body and an ON SIZE ERROR phrase.
    [InlineData("SQG14", "MAIN-PARA.\n    PERFORM 2 TIMES GO TO P2 DISPLAY \"A\" END-PERFORM.\nP2.\n    STOP RUN.\n")]
    [InlineData("SQG15", "MAIN-PARA.\n    ADD 1 TO W-N ON SIZE ERROR GO TO P2 DISPLAY \"A\" END-ADD.\nP2.\n    STOP RUN.\n")]
    public void Format1GoTo_FollowedByAStatement_IsRefused(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions)
            EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Program(pid, procedure), edition), GoToNotLast);
    }

    // ── §14.9.42.3 SR1 ───────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SQS01", "MAIN-PARA.\n    DISPLAY \"A\" STOP RUN.\n")]
    // Last of a THEN phrase followed by an ELSE phrase, and followed by statements after END-IF.
    [InlineData("SQS02", "MAIN-PARA.\n    IF W-N = 0 STOP RUN ELSE DISPLAY \"A\" END-IF.\n    STOP RUN.\n")]
    [InlineData("SQS03", "MAIN-PARA.\n    IF W-N = 0 DISPLAY \"A\" STOP RUN END-IF DISPLAY \"B\".\n    STOP RUN.\n")]
    public void Stop_LastInItsBlock_IsAccepted(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diagnostics) = EditionHarness.Compile(Program(pid, procedure), edition);
            EditionHarness.AssertNoDiagnostic(diagnostics, StopNotLast);
            Assert.True(ok, $"{pid} @{edition}: {string.Join(" | ", diagnostics)}");
        }
    }

    [Theory]
    [InlineData("SQS11", "MAIN-PARA.\n    DISPLAY \"A\" STOP RUN DISPLAY \"B\".\n")]
    [InlineData("SQS12", "MAIN-PARA.\n    IF W-N = 0 STOP RUN DISPLAY \"A\" END-IF.\n    STOP RUN.\n")]
    [InlineData("SQS13", "MAIN-PARA.\n    PERFORM 2 TIMES STOP RUN DISPLAY \"A\" END-PERFORM.\n    STOP RUN.\n")]
    public void Stop_FollowedByAStatement_IsRefused(string pid, string procedure)
    {
        foreach (int edition in EditionHarness.Editions)
            EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Program(pid, procedure), edition), StopNotLast);
    }

    /// <summary>The X3.23-1985 STOP literal (deleted 2002) is NOT the statement §14.9.42.3 SR1 governs: §14.9.42 has
    /// only STOP RUN, which ends the run unit, while STOP literal communicates to the operator and then CONTINUES with
    /// the next statement — so the statement after it executes and the rule's reason does not hold. Found by the
    /// train-1031 lander's review: this case was refused at 85, legal source the edition runs.</summary>
    [Fact]
    public void StopLiteral_FollowedByAStatement_IsAccepted()
    {
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("SQS21", "MAIN-PARA.\n    STOP \"HALT\" DISPLAY \"A\".\n    STOP RUN.\n"), 85);
        EditionHarness.AssertNoDiagnostic(diagnostics, StopNotLast);
        Assert.True(ok, $"SQS21 @85: {string.Join(" | ", diagnostics)}");
    }

    // ── Source shape ─────────────────────────────────────────────────────────────────────────────────────

    private static string Program(string pid, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(2) VALUE 0.
        PROCEDURE DIVISION.
        {procedure.Replace("\n", "\n    ")}
        """;
}
