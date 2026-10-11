// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB2847 — every not-found path tells the user WHY the module was not found, and a probe's reason belongs to
/// the lookup that made it. The sibling-module probe (<c>ProgramTable.ProbeSiblingModule</c>, the implementor-defined
/// part of how the runtime system "attempts to locate" a program, ISO §14.9.4.4 GR3b) finds <c>&lt;name&gt;.dll</c> beside
/// the run; when that module is present but does not carry the name, the miss is "set to exist" (§8.4.3.13.4 GR4 for a
/// program-address-identifier, §8.4.3.12.4 GR4 for a function-address-identifier, cite.py OK) and its reason rides the
/// termination message the fatal default prints (§14.6.13.1.3 7)).
/// <para>The companion <c>PROGRAM-ID. P2847Q AS "P2847R"</c> compiles to <c>P2847Q.dll</c> but registers only the
/// externalized name P2847R (§11.10.4 GR1), so a lookup of P2847Q finds the module and then not the program: a reason
/// every surface must carry. CALL and ADDRESS OF PROGRAM carried it; <c>SET … TO ENTRY</c> and
/// <c>ADDRESS OF FUNCTION</c> dropped it, and the probe's failure table was keyed by the name alone, so the reason of
/// a FUNCTION lookup of a name could be printed for a PROGRAM lookup of it.</para>
/// </summary>
public sealed class NotLocatedReasonTests
{
    /// <summary>Names the module <c>P2847Q.dll</c> but registers only <c>P2847R</c>.</summary>
    private const string ProgramModule = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P2847Q AS "P2847R".
        PROCEDURE DIVISION.
        MAIN.
            GOBACK.
        END PROGRAM P2847Q.
        """;

    /// <summary>A module that carries the PROGRAM P2847PC — so a FUNCTION lookup of that name finds the file and not the function.</summary>
    private const string ProgramModuleP2847PC = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P2847PC.
        PROCEDURE DIVISION.
        MAIN.
            GOBACK.
        END PROGRAM P2847PC.
        """;

    private const string Function = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. P2847FD.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-ARG PIC S9(4).
        01 L-RES PIC S9(9).
        PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
            COMPUTE L-RES = L-ARG * 2
            GOBACK.
        END FUNCTION P2847FD.
        """;

    private const string Callee = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P2847CALLEE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-PTR USAGE PROGRAM-POINTER.
        PROCEDURE DIVISION USING L-PTR.
        MAIN.
            GOBACK.
        END PROGRAM P2847CALLEE.
        """;

    /// <summary>One caller program: the working-storage every case needs, then <paramref name="procedure"/>.</summary>
    private static string Caller(string programId, string name, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {programId}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION P2847FD.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 PP USAGE PROGRAM-POINTER.
        01 FPD USAGE FUNCTION-POINTER TO P2847FD.
        01 WS-NAME PIC X(12) VALUE "{name}".
        PROCEDURE DIVISION.
        MAIN.
        {procedure}
            STOP RUN.
        END PROGRAM {programId}.
        {Function}
        {Callee}
        """;

    private static (int exit, string stdout, string stderr) Run(string source, params string[] companions)
        => new CobolNetCompiler(2023).CompileAndRunExit(source, companions);

    private static void AssertNotLocatedWithReason(
        (int exit, string stdout, string stderr) r, string ec, string surface, string kind, string name)
    {
        Assert.Equal(1, r.exit);                                                        // §14.6.12 — the abnormal-termination exit
        Assert.Equal("BEFORE", r.stdout);                                               // nothing after the failing statement runs
        Assert.Contains("abnormal run-unit termination", r.stderr);
        Assert.Contains(ec, r.stderr);
        Assert.Contains(surface, r.stderr);                                             // which operation missed
        Assert.Contains($"; the sibling module was not usable: ", r.stderr);            // the probe's reason is carried ...
        Assert.Contains($"does not carry the {kind} {name}", r.stderr);                 // ... and it is the reason for THIS kind of lookup
    }

    /// <summary>§8.4.3.13.4 GR4 — every surface of a program-address-identifier miss gives the probe's reason: the
    /// SET statement (identifier form of ADDRESS OF PROGRAM, and the vendor TO ENTRY), a relation operand and a CALL
    /// argument. The first two were the dropped-reason arms (kb/Work PB2847); the others are the arms that already
    /// carried it and stay pinned beside them.</summary>
    [Theory]
    [InlineData("SET PP TO ADDRESS OF PROGRAM WS-NAME", "ADDRESS OF PROGRAM 'P2847Q'")]
    [InlineData("SET PP TO ENTRY WS-NAME", "ADDRESS OF PROGRAM 'P2847Q'")]
    [InlineData("IF ADDRESS OF PROGRAM WS-NAME = NULL DISPLAY \"EQ\" END-IF", "ADDRESS OF PROGRAM 'P2847Q'")]
    [InlineData("CALL \"P2847CALLEE\" USING ADDRESS OF PROGRAM WS-NAME", "ADDRESS OF PROGRAM 'P2847Q'")]
    [InlineData("CALL WS-NAME", "CALL 'P2847Q'")]
    public void ProgramMiss_NamesTheModulesReason_OnEverySurface(string statement, string surface)
    {
        string source = Caller("P2847A", "P2847Q", $"""
                >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
                DISPLAY "BEFORE"
                {statement}
                DISPLAY "AFTER"
            """);
        AssertNotLocatedWithReason(Run(source, ProgramModule), "EC-PROGRAM-NOT-FOUND", surface, "program", "P2847Q");
    }

    /// <summary>§8.4.3.12.4 GR4 — the function-address-identifier miss gives the probe's reason too (the dropped-reason
    /// arm of kb/Work PB2847), and it is the FUNCTION lookup's reason: the file P2847PC.dll exists and carries a program
    /// of that name, not a function.</summary>
    [Fact]
    public void FunctionMiss_NamesTheModulesReason()
    {
        string source = Caller("P2847B", "P2847PC", """
                >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
                DISPLAY "BEFORE"
                SET FPD TO ADDRESS OF FUNCTION WS-NAME
                DISPLAY "AFTER"
            """);
        AssertNotLocatedWithReason(Run(source, ProgramModuleP2847PC), "EC-FUNCTION-NOT-FOUND",
            "ADDRESS OF FUNCTION 'P2847PC'", "function", "P2847PC");
    }

    /// <summary>The probe's failure table is keyed by the externalized name AND the kind of unit asked for (kb/Work
    /// PB2847; §8.4.6.3's first paragraph makes a program-name and a function-name different kinds of name). An
    /// UNCHECKED program lookup and an UNCHECKED function lookup of the one name each probe the module and record their
    /// own reason (a miss is not raised while its checking is off, §14.6.13.1.4); the CALL that then misses, with the
    /// program lookup's probe already made and cached, must print the PROGRAM reason — before the fix the function
    /// lookup's reason, recorded last under the bare name, was printed for it.</summary>
    [Fact]
    public void AProbeReason_BelongsToTheKindOfLookupThatMadeIt()
    {
        string source = Caller("P2847C", "P2847Q", """
                DISPLAY "BEFORE"
                SET PP TO ADDRESS OF PROGRAM WS-NAME
                SET FPD TO ADDRESS OF FUNCTION WS-NAME
                >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
                CALL WS-NAME
                DISPLAY "AFTER"
            """);
        var r = Run(source, ProgramModule);
        AssertNotLocatedWithReason(r, "EC-PROGRAM-NOT-FOUND", "CALL 'P2847Q'", "program", "P2847Q");
        Assert.DoesNotContain("does not carry the function", r.stderr);
    }
}
