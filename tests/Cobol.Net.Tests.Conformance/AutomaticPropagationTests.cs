// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// <c>&gt;&gt;PROPAGATE ON</c>'s automatic propagation (kb/Work PB1119) on the arms the corpus golden
/// <c>2002/w73d2_pb1119_propagate_on</c> cannot hold — a golden must exit 0, and two arms here end in abnormal
/// termination — plus the METHOD, I-O and exception-OBJECT arms.
/// <list type="bullet">
/// <item>ISO §14.6.13.1.3 6): "If checking for the exception condition is enabled, and the exception condition is
/// neither EC-FLOW-GLOBAL-EXIT nor EC-FLOW-GLOBAL-GOBACK, and there is an applicable PROPAGATE ON directive, the
/// exception condition is propagated as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were
/// executed." It follows 5) — a declarative that completes normally still terminates the run unit.</item>
/// <item>§7.3.21.1: the directive causes "propagation of exception conditions to the activating runtime element",
/// and a main program has none (§14.9.18.4 GR3: its GOBACK is a STOP that ignores RAISING), so 7)'s termination
/// stands there — the determination recorded in docs/CONFORMANCE.md.</item>
/// <item>§14.6.13.1.5, EXIT/GOBACK item 3: under PROPAGATE ON the ACTIVATOR re-propagates an exception object no
/// declarative took, as the object when its header's RAISING phrase names an applicable class, otherwise as
/// EXCEPTION EC-OO-EXCEPTION.</item>
/// </list>
/// </summary>
public sealed class AutomaticPropagationTests
{
    /// <summary>§14.6.13.1.3 5) precedes 6): the callee's own declarative runs and completes normally, so the run
    /// unit terminates abnormally even though the callee is under PROPAGATE ON — nothing reaches the caller.</summary>
    [Fact]
    public void DeclarativeCompletesNormally_TerminatesInsteadOfPropagating()
    {
        const string source = """
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T1A.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION.
                USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
                DISPLAY "A CAUGHT".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
                CALL "W73D2T1B".
                DISPLAY "A AFTER".
                STOP RUN.
            END PROGRAM W73D2T1A.
            >>PROPAGATE ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T1B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 E PIC X OCCURS 3.
            01 I PIC 99 VALUE 7.
            PROCEDURE DIVISION.
            DECLARATIVES.
            DB SECTION.
                USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
                DISPLAY "B DECLARATIVE COMPLETES".
            END DECLARATIVES.
            MAIN SECTION.
                MOVE "1" TO E(I).
                DISPLAY "B AFTER".
                GOBACK.
            END PROGRAM W73D2T1B.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        Assert.Equal("B DECLARATIVE COMPLETES", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains("EC-BOUND-SUBSCRIPT", stderr);
    }

    /// <summary>A MAIN program under PROPAGATE ON has no activating element to propagate to (§7.3.21.1), so an
    /// unhandled fatal condition terminates the run unit (§14.6.13.1.3 7)) — never the normal STOP a literal
    /// GOBACK would be (§14.9.18.4 GR3).</summary>
    [Fact]
    public void MainProgram_HasNoActivator_Terminates()
    {
        const string source = """
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            >>PROPAGATE ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 E PIC X OCCURS 3.
            01 I PIC 99 VALUE 7.
            PROCEDURE DIVISION.
                DISPLAY "BEFORE".
                MOVE "1" TO E(I).
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM W73D2T2.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        Assert.Equal("BEFORE", stdout);
        Assert.Equal(1, exit);
        Assert.Contains("EC-BOUND-SUBSCRIPT", stderr);
    }

    /// <summary>The METHOD arm (a method is one of §7.3.21.4 GR1's "functions, methods, and programs") and the EC-I-O
    /// arm (§14.6.13.1.3 3) sends a fatal EC-I-O condition on to 6)): each propagates to its activator, whose
    /// declarative resumes.</summary>
    [Fact]
    public void MethodAndIoArms_PropagateToTheActivator()
    {
        const string source = """
            >>TURN EC-BOUND-SUBSCRIPT EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T3M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS W73D2C3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE W73D2C3.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION.
                USE AFTER EXCEPTION CONDITION EC-ALL.
                DISPLAY "M CAUGHT " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
                INVOKE W73D2C3 "NEW" RETURNING S.
                INVOKE S "WORK".
                DISPLAY "M AFTER INVOKE".
                CALL "W73D2T3P".
                DISPLAY "M AFTER CALL".
                STOP RUN.
            END PROGRAM W73D2T3M.
            >>PROPAGATE ON
            IDENTIFICATION DIVISION.
            CLASS-ID. W73D2C3 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 E PIC X OCCURS 3.
            01 I PIC 99 VALUE 7.
            PROCEDURE DIVISION.
            METHOD-ID. WORK.
            PROCEDURE DIVISION.
            MAIN.
                MOVE "1" TO E(I).
                DISPLAY "WORK AFTER".
                GOBACK.
            END METHOD WORK.
            END OBJECT.
            END CLASS W73D2C3.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T3P.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "w73d2-t3-no-such-file.dat"
                    ORGANIZATION SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 R PIC X(10).
            PROCEDURE DIVISION.
                OPEN INPUT F.
                DISPLAY "P AFTER OPEN".
                GOBACK.
            END PROGRAM W73D2T3P.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        Assert.True(exit == 0, stderr);
        Assert.Equal(
            "M CAUGHT EC-BOUND-SUBSCRIPT\nM AFTER INVOKE\n"
            + "M CAUGHT EC-I-O-PERMANENT-ERROR\nM AFTER CALL",
            stdout);
    }

    /// <summary>§14.6.13.1.5 EXIT/GOBACK item 3, both halves: W73D2T4A's header RAISING names the object's class, so
    /// it re-propagates the OBJECT (the main program's Format-4 declarative takes it); W73D2T4B's names none, so it
    /// propagates EXCEPTION EC-OO-EXCEPTION (the main program's Format-3 declarative takes it). Before kb/Work PB1119
    /// both fell to item 4 in the middle program and terminated the run unit there.</summary>
    [Fact]
    public void ExceptionObject_ItemThree_ObjectOrEcOoException()
    {
        const string source = """
            >>TURN EC-OO-EXCEPTION CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T4M.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS W73D2E4.
            PROCEDURE DIVISION.
            DECLARATIVES.
            OBJ-SEC SECTION.
                USE AFTER EXCEPTION OBJECT W73D2E4.
                DISPLAY "M HANDLED OBJECT".
            NAME-SEC SECTION.
                USE AFTER EXCEPTION CONDITION EC-OO-EXCEPTION.
                DISPLAY "M CAUGHT " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
                CALL "W73D2T4A".
                DISPLAY "M AFTER A".
                CALL "W73D2T4B".
                DISPLAY "M AFTER B".
                STOP RUN.
            END PROGRAM W73D2T4M.
            IDENTIFICATION DIVISION.
            CLASS-ID. W73D2E4 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            END CLASS W73D2E4.
            IDENTIFICATION DIVISION.
            CLASS-ID. W73D2S4 INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS W73D2E4.
            IDENTIFICATION DIVISION.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-E USAGE OBJECT REFERENCE W73D2E4.
            PROCEDURE DIVISION.
            METHOD-ID. WORK.
            PROCEDURE DIVISION RAISING W73D2E4.
            MAIN.
                INVOKE W73D2E4 "NEW" RETURNING W-E.
                GOBACK RAISING W-E.
            END METHOD WORK.
            END OBJECT.
            END CLASS W73D2S4.
            >>PROPAGATE ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T4A.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS W73D2E4
                CLASS W73D2S4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE W73D2S4.
            PROCEDURE DIVISION RAISING W73D2E4.
                INVOKE W73D2S4 "NEW" RETURNING S.
                INVOKE S "WORK".
                DISPLAY "A AFTER INVOKE".
                GOBACK.
            END PROGRAM W73D2T4A.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W73D2T4B.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS W73D2S4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S USAGE OBJECT REFERENCE W73D2S4.
            PROCEDURE DIVISION.
                INVOKE W73D2S4 "NEW" RETURNING S.
                INVOKE S "WORK".
                DISPLAY "B AFTER INVOKE".
                GOBACK.
            END PROGRAM W73D2T4B.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        Assert.True(exit == 0, stderr);
        Assert.Equal(
            "M HANDLED OBJECT\nM AFTER A\nM CAUGHT EC-OO-EXCEPTION\nM AFTER B",
            stdout);
    }
}
