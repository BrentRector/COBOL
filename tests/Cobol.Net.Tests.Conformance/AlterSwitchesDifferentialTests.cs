// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ALTER + the 85-only target-less GO TO (ANSI X3.23-1985; deleted by ISO/IEC 1989:2002 — the 2023 §14.9.17 GO TO
/// has only Formats 1–2) and the SPECIAL-NAMES external-switch family (ISO §12.3.7; SET F3 §14.9.39; switch-status
/// condition §8.8.4.6). Differential against the committed golden (NIST-85 green: NC174A/254A/302M/303M); the
/// edition-gating fact uses the per-edition harness. Switch facts avoid the guard's COBOL_SWITCH_1 env contract by
/// using SWITCH-31…SWITCH-34 (available switch-names, docs/CONFORMANCE.md §7 item 191, that no environment sets ⇒
/// deterministic default OFF in both engines; they were SWTEST-* until kb/Work PB862 closed the name set).
/// </summary>
public sealed class AlterSwitchesDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>A program with the two-switch SPECIAL-NAMES surface NC174A/NC254A exercise: Option 1 with the
    /// STATUS keyword and Option 1 with the keyword-less <c>ON IS</c>/<c>OFF IS</c> shape (§12.3.7 — one format).</summary>
    private static string SwitchProgram(string programId, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{programId}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SWITCH-31 IS SWM-A
                ON STATUS IS A-IS-ON
                OFF STATUS IS A-IS-OFF
            SWITCH-32 IS SWM-B
                ON IS B-IS-ON
                OFF IS B-IS-OFF.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-N PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
        """;

    [Fact]   // §12.3.7 GR2 (status interrogated per §8.8.4.6 GR1; no external setting ⇒ OFF) + §14.9.39 F3 GR5.
    public void Switch_DefaultOff_ThenSetToOn_FlipsBothStatusConditions()
        => AssertMatchesGolden(SwitchProgram("SWTST1", """
                IF A-IS-OFF DISPLAY "A-OFF-DEFAULT".
                IF A-IS-ON DISPLAY "A-ON-DEFAULT".
                SET SWM-A TO ON.
                IF A-IS-ON DISPLAY "A-ON-AFTER-SET".
                IF A-IS-OFF DISPLAY "A-OFF-AFTER-SET".
                STOP RUN.
            """));

    [Fact]   // §14.9.39 F3: the mnemonic LIST repeats (SET A B TO ON) and the OUTER group repeats (SET A TO OFF B TO ON).
    public void Switch_MultiReceiverAndCompoundSet_EachGroupTakesItsOwnPosition()
        => AssertMatchesGolden(SwitchProgram("SWTST2", """
                SET SWM-A SWM-B TO ON.
                IF A-IS-ON DISPLAY "MULTI-A-ON".
                IF B-IS-ON DISPLAY "MULTI-B-ON".
                SET SWM-A TO OFF SWM-B TO ON.
                IF A-IS-OFF DISPLAY "GROUP-A-OFF".
                IF B-IS-ON DISPLAY "GROUP-B-STILL-ON".
                STOP RUN.
            """));

    [Fact]   // §12.3.7 Option 2 (no mnemonic — only status condition-names; the switch cannot be SET, GR3/SR5).
    public void Switch_Option2NoMnemonic_StatusConditionsResolve()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SWTST3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SWITCH-33 ON STATUS IS C-IS-ON OFF STATUS IS C-IS-OFF.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF C-IS-OFF DISPLAY "C-OFF".
                IF C-IS-ON DISPLAY "C-ON".
                STOP RUN.
            """);

    [Fact]   // §8.8.4.4 lists switch-status among the SIMPLE conditions: AND/OR/NOT combinable (the NC254A shape),
             // and as a complete simple condition it TERMINATES an abbreviated sequence (§8.8.4.12.4 GR1) — the
             // trailing A-IS-ON in `WS-N = 9 OR A-IS-ON` is the switch test, NOT an inserted-subject relation.
    public void Switch_InCompoundAndAbbreviatedConditions()
        => AssertMatchesGolden(SwitchProgram("SWTST4", """
                SET SWM-A TO ON.
                IF A-IS-ON AND B-IS-OFF DISPLAY "AND-OK".
                IF B-IS-ON OR A-IS-ON DISPLAY "OR-OK".
                IF NOT A-IS-OFF DISPLAY "NOT-OK".
                IF WS-N = 9 OR A-IS-ON DISPLAY "MIXED-OK".
                STOP RUN.
            """));

    [Fact]   // Resolution order (NC211A regression guard): a name defined BOTH as a SPECIAL-NAMES status
             // condition-name and as a level-88 resolves as the LEVEL-88 (§8.8.4.5 over §8.8.4.6 in this
             // implementation's name resolution — the legacy NIST-proven order).
    public void Switch_Level88WinsOverSwitchStatusName()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SWTST5.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SWITCH-31 IS SWM-A ON STATUS IS DUAL-NAME.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC 9 VALUE 0.
                88 DUAL-NAME VALUE 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET SWM-A TO ON.
                IF DUAL-NAME DISPLAY "T1-TRUE" ELSE DISPLAY "T1-FALSE".
                MOVE 1 TO WS-FLAG.
                IF DUAL-NAME DISPLAY "T2-TRUE" ELSE DISPLAY "T2-FALSE".
                STOP RUN.
            """);

    [Fact]   // The implementor external facility (ISO implementor-defined item 191 / §12.3.7 GR4): the initial
             // status comes from COBOL_<SWITCH-NAME> (dashes→underscores), value ON ⇒ on — both engines share
             // the contract (the NIST guard exports COBOL_SWITCH_1=ON on the same basis).
    public void Switch_EnvironmentVariableSuppliesInitialState()
    {
        Environment.SetEnvironmentVariable("COBOL_SWITCH_34", "ON");
        try
        {
            AssertMatchesGolden("""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. SWTST6.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                SPECIAL-NAMES.
                    SWITCH-34 IS SWM-E
                        ON STATUS IS E-IS-ON
                        OFF STATUS IS E-IS-OFF.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    IF E-IS-ON DISPLAY "ENV-ON".
                    IF E-IS-OFF DISPLAY "ENV-OFF".
                    SET SWM-E TO OFF.
                    IF E-IS-OFF DISPLAY "SET-BEATS-ENV".
                    STOP RUN.
                """);
        }
        finally { Environment.SetEnvironmentVariable("COBOL_SWITCH_34", null); }
    }

    [Fact]   // ANSI X3.23-1985 ALTER GR: until an ALTER executes, the WRITTEN GO TO target governs (the D4 field's
             // initial value); after, the GO TO transfers to the ALTER's destination.
    public void Alter_WrittenTargetGovernsUntilAltered()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTT1.
            PROCEDURE DIVISION.
            MAIN-P.
                GO TO GATE-P.
            GATE-P.
                GO TO FIRST-P.
            FIRST-P.
                DISPLAY "WRITTEN-TARGET".
                ALTER GATE-P TO PROCEED TO SECOND-P.
                GO TO GATE-P.
            SECOND-P.
                DISPLAY "ALTERED-TARGET".
                STOP RUN.
            """);

    [Fact]   // Multi-entry ALTER with PROCEED omitted on one entry (the NC302M shape) and a comma separator
             // between entries (the NC303M shape) — one statement, both targets re-pointed.
    public void Alter_MultiEntry_CommaSeparated_ProceedOptional()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTT2.
            PROCEDURE DIVISION.
            MAIN-P.
                ALTER G1-P TO T1-P, G2-P TO PROCEED TO T2-P.
                GO TO G1-P.
            G1-P.
                GO TO DEAD-P.
            G2-P.
                GO TO DEAD-P.
            DEAD-P.
                DISPLAY "DEAD".
                STOP RUN.
            T1-P.
                DISPLAY "T1".
                GO TO G2-P.
            T2-P.
                DISPLAY "T2".
                STOP RUN.
            """);

    [Fact]   // The 85-only target-less GO TO. — legal ONLY in a single-GO-TO paragraph named by an ALTER, and it
             // must be ALTERed before execution (ANSI X3.23-1985; deleted by 2002).
    public void Alter_BareGoTo_AlteredBeforeExecution()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTT3.
            PROCEDURE DIVISION.
            MAIN-P.
                ALTER GATE-P TO PROCEED TO DONE-P.
                GO TO GATE-P.
            GATE-P.
                GO TO.
            FALL-P.
                DISPLAY "FELL-THROUGH".
                STOP RUN.
            DONE-P.
                DISPLAY "BARE-ALTERED".
                STOP RUN.
            """);

    [Fact]   // ALTER inside PERFORM … THRU dispatch (the NC302M execution shape) + the MOST RECENT ALTER governs
             // subsequent transfers (ANSI-85 ALTER GR) — written default, altered, then re-altered.
    public void Alter_InsidePerformThru_LastAlterWins()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTT4.
            PROCEDURE DIVISION.
            MAIN-P.
                PERFORM GATE-P THRU EXIT-P.
                ALTER GATE-P TO PROCEED TO B-P.
                PERFORM GATE-P THRU EXIT-P.
                ALTER GATE-P TO PROCEED TO A-P.
                PERFORM GATE-P THRU EXIT-P.
                STOP RUN.
            GATE-P.
                GO TO A-P.
            A-P.
                DISPLAY "A".
                GO TO EXIT-P.
            B-P.
                DISPLAY "B".
                GO TO EXIT-P.
            EXIT-P.
                EXIT.
            """);

    [Fact]   // Edition gating (the four-compilers rule): ALTER and the target-less GO TO compile at --std 85
             // (obsolete elements there — no failing diagnostic) and are REJECTED at 2002/2014/2023 as deleted
             // (ISO/IEC 1989:2002 deletion; 2023 §14.9.17 has no altered form).
    public void Alter_And_BareGoTo_RejectedAt2002Plus_AcceptedAt85()
    {
        const string alterSrc = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTED1.
            PROCEDURE DIVISION.
            MAIN-P.
                ALTER GATE-P TO PROCEED TO DONE-P.
                GO TO GATE-P.
            GATE-P.
                GO TO DONE-P.
            DONE-P.
                STOP RUN.
            """;
        const string bareGoToSrc = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTED2.
            PROCEDURE DIVISION.
            MAIN-P.
                STOP RUN.
            GATE-P.
                GO TO.
            DONE-P.
                STOP RUN.
            """;
        Assert.True(EditionHarness.Compile(alterSrc, 85).Ok, "ALTER must compile at --std 85");
        Assert.True(EditionHarness.Compile(bareGoToSrc, 85).Ok, "the target-less GO TO must compile at --std 85");
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (alterOk, alterDiags) = EditionHarness.Compile(alterSrc, edition);
            Assert.False(alterOk, $"ALTER must be rejected at --std {edition}");
            EditionHarness.AssertHasDiagnostic(alterDiags, "ALTER");
            var (bareOk, bareDiags) = EditionHarness.Compile(bareGoToSrc, edition);
            Assert.False(bareOk, $"the target-less GO TO must be rejected at --std {edition}");
            EditionHarness.AssertHasDiagnostic(bareDiags, "GO TO");
        }
    }
}
