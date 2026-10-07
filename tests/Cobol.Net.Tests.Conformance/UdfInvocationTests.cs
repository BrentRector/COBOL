// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// User-defined function invocation (ISO §9.4 / §8.4.3.2 / §12.3.8; Phase 4c M2-UDF-1 — DEVLOG 615).
/// End-to-end behavior rides the three udf_* conformance goldens (COMPUTE/MOVE receiving forms, inline
/// sub-expression, literal + arithmetic-expression arguments — all byte-exact); these lock the edition
/// gate, the §12.3.8.2 GR12 repository semantics, and the COBOLNET1501/1505–1509 diagnostic band.
/// </summary>
public sealed class UdfInvocationTests
{
    /// <summary>A whole-source caller + FUNCTION-ID unit. <paramref name="pid"/> keeps PROGRAM-IDs unique
    /// per fact (the stale same-named-assembly hazard); {body} is the caller's MAIN body.</summary>
    private static string Group(string pid, string body, string repository = "    FUNCTION UDFDBL.") => $$"""
        IDENTIFICATION DIVISION.
        FUNCTION-ID. UDFDBL.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        P.
            COMPUTE L-R = L-X * 2.
            GOBACK.
        END FUNCTION UDFDBL.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
        {{repository}}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 4.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
            STOP RUN.
        END PROGRAM {{pid}}.
        """;

    /// <summary>The introduction gate: user-defined functions are COBOL-2002+ (§9.4 / §12.3.8) — 0900 at 85
    /// (the binder gate and the PD-header RETURNING parse hint both name the edition); binds at 2002.</summary>
    [Fact]
    public void Invocation_IntroducedAt2002()
    {
        string src = Group("UDFT1", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).");
        var (ok85, e85, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok85, "user-defined functions are 2002+; 85 must reject");
        EditionHarness.AssertHasDiagnostic(e85, "COBOLNET0900");
        var (ok02, e02, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok02, "must bind at 2002: " + string.Join("\n", e02));
    }

    /// <summary>§12.3.8.2 GR12 is a PRECONDITION: without the repository FUNCTION specifier the name is not
    /// a user-function reference — COBOLNET1501, with the hint naming the in-group FUNCTION-ID.</summary>
    [Fact]
    public void MissingRepositoryEntry_1501_WithGr12Hint()
    {
        string src = Group("UDFT2", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).",
            repository: "    FUNCTION ALL INTRINSIC.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1501");
        Assert.Contains(errors, e => e.Contains("REPOSITORY", StringComparison.Ordinal));
    }

    /// <summary>A REPOSITORY-declared function with NO in-group FUNCTION-ID definition is the
    /// separate-compilation prototype surface (M2-UDF-3) — staged loud as COBOLNET1505.</summary>
    [Fact]
    public void DeclaredButUndefined_PrototypeGap_1505()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION NOWHERE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION NOWHERE(1).
                STOP RUN.
            END PROGRAM UDFT3.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1505");
    }

    /// <summary>§14.8.2 positional correspondence: an argument-count mismatch is COBOLNET1506. (The
    /// zero-argument spelling is the bare name — empty parentheses are a §8.4.3.2 format violation and
    /// already fail at parse.)</summary>
    [Theory]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL.")]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(1, 2).")]
    public void ArityMismatch_1506(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT4", body), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1506");
    }

    /// <summary>§14.2 procedure-division header (:23666): "The RETURNING phrase shall be specified in a
    /// function definition" — checked once per unit, even for an uncalled function (COBOLNET1507).</summary>
    [Fact]
    public void FunctionWithoutReturning_1507()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFNORET.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION.
            P.
                GOBACK.
            END FUNCTION UDFNORET.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT5.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT5.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1507");
    }

    /// <summary>Per-evaluation activation (P10 Step 10 — §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2): a function
    /// reference in a conditionally- or repeatedly-evaluated CONDITION window now BINDS — the activations
    /// attach to the condition (<c>BoundUdfEvaluated</c>) and run once per evaluation of that window.
    /// Legs: PERFORM UNTIL (per iteration, §14.9.28 GR6), PERFORM VARYING UNTIL (GR13), an EVALUATE
    /// selection OBJECT (per WHEN consideration, §14.9.13.4 GR4), and a non-first AND/OR operand
    /// (§8.8.4.13 r1 short-circuit). Runtime cardinality is proven by the udf_per_eval golden.</summary>
    [Theory]
    [InlineData("UDFT6A", "    PERFORM UNTIL FUNCTION UDFDBL(WS-A) > 9000\n        ADD 1 TO WS-A\n    END-PERFORM.")]
    [InlineData("UDFT6B", "    PERFORM VARYING WS-A FROM 1 BY 1 UNTIL WS-A > FUNCTION UDFDBL(2)\n        DISPLAY \"X\"\n    END-PERFORM.")]
    [InlineData("UDFT6C", "    EVALUATE WS-A\n        WHEN FUNCTION UDFDBL(2) DISPLAY \"E\"\n    END-EVALUATE.")]
    [InlineData("UDFT6D", "    IF WS-A = 4 AND FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    [InlineData("UDFT6E", "    IF WS-A = 9 OR FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    public void ConditionallyEvaluatedPositions_BindPerEvaluation(string pid, string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group(pid, body), 2002);
        Assert.True(ok, "per-evaluation window must bind: " + string.Join("\n", errors));
    }

    /// <summary>The two PERFORM VARYING OPERAND windows §14.9.28.4 GR12 names — a BY operand (read at every
    /// augment) and an AFTER level's FROM operand (re-read at every outer-augment re-initialization) — BIND,
    /// and carry NO 1509 stage.
    /// <para>⛔ THIS THEORY USED TO ASSERT THE OPPOSITE, AND THAT IS THE POINT (kb/Work PB437). It pinned
    /// <c>Assert.False(ok)</c> + COBOLNET1509 on both rows, green, for source the standard admits — the same
    /// construct the FIRST-level FROM one position over and the UNTIL condition in the same statement both
    /// accepted, and the diagnostic's own text said the refusal was "an activation cardinality this
    /// implementation does not yet realize", i.e. a capability limit and not a rule. A green test pinning a
    /// loud stage reads as a DECISION that the refusal is correct
    /// (feedback_green_test_can_hold_a_gap_open), which is exactly how it survived; the EVALUATE selection
    /// subject left the same list for the same reason (kb/Work PB394, the Fact below). The operands now ride
    /// <c>BoundUdfEvaluatedExpr</c>, the expression twin of the carrier the conditions already used, and the
    /// RUN-TIME cardinality proof — one activation per setting/augmenting operation — is the
    /// <c>pb437_varying_operand_windows</c> golden.</para></summary>
    [Theory]
    [InlineData("UDFT6F", "    PERFORM VARYING WS-A FROM 1 BY FUNCTION UDFDBL(1) UNTIL WS-A > 9\n        DISPLAY \"X\"\n    END-PERFORM.")]
    [InlineData("UDFT6G", "    PERFORM VARYING WS-A FROM 1 BY 1 UNTIL WS-A > 3\n            AFTER WS-R FROM FUNCTION UDFDBL(1) BY 1 UNTIL WS-R > 3\n        DISPLAY \"X\"\n    END-PERFORM.")]
    public void PerEvaluationOperandWindows_Bind(string pid, string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group(pid, body), 2002);
        Assert.True(ok, "a per-evaluation OPERAND window must bind: " + string.Join("\n", errors));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1509", StringComparison.Ordinal));
    }

    /// <summary>A user-function EVALUATE selection SUBJECT compiles, and carries NO 1509 stage: ISO §14.9.13.4
    /// GR3 evaluates each selection subject once "at the beginning of the execution of the EVALUATE statement",
    /// which is a statement-scope hoist, not a per-WHEN window (kb/Work PB394). One WHEN renders the value
    /// inline (one render already IS one evaluation); more than one materializes it into the intermediate result
    /// item, so the activation count is one either way. The RUN-TIME cardinality proof is the
    /// <c>pb394_sending_value_evaluated_once</c> golden at four editions; this fact is the compile-clean half,
    /// and it is what the removed 1509 row used to deny.</summary>
    [Fact]
    public void EvaluateValueSubject_HoistsToStatementScope_NoResidue()
    {
        const string body = "    EVALUATE FUNCTION UDFDBL(2)\n        WHEN 4 DISPLAY \"E\"\n    END-EVALUATE.";
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT6H", body), 2002);
        Assert.True(ok, "a user-function EVALUATE subject must bind: " + string.Join("\n", errors));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1509", StringComparison.Ordinal));
    }

    /// <summary>The SEARCH WHEN window binds per-evaluation too (per scan pass — §14.9.37.4 GR1, "Any subscripting specified in a WHEN phrase is evaluated each time the conditions in that WHEN phrase are evaluated", over GR4's "The process is then repeated using the new index setting") — the
    /// runtime pass-count proof is the udf_per_eval golden's SEARCH leg.</summary>
    [Fact]
    public void SearchWhenCondition_BindsPerEvaluation()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                GOBACK.
            END FUNCTION UDFDBL.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT6S.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFDBL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TAB.
               05 ROW PIC 9(2) OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION.
            MAIN.
                SET IX TO 1.
                SEARCH ROW
                    WHEN ROW(IX) = FUNCTION UDFDBL(2) DISPLAY "F"
                END-SEARCH.
                STOP RUN.
            END PROGRAM UDFT6S.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, "SEARCH WHEN must bind per-evaluation: " + string.Join("\n", errors));
    }

    /// <summary>The 0900 introduction gate on the BINDER path: a caller-only source (no FUNCTION-ID unit,
    /// so the {is2002()}? PD-header RETURNING parse hint cannot be the source of the diagnostic) still names
    /// the edition at 85 via ConstructRegistry.Check in UdfBindCall.</summary>
    [Fact]
    public void BinderGate_0900_At85_CallerOnly()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT10.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION NOWHERE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION NOWHERE(1).
                STOP RUN.
            END PROGRAM UDFT10.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
    }

    /// <summary>Two FUNCTION-ID definitions with one name. ⛔ The DIAGNOSTIC CHANGED with kb/Work PB660 and the
    /// old one was citing the wrong clause: COBOLNET1508 keyed on the declared WORD and cited §8.4.6.6, the
    /// scope of function-prototype-NAMES, which says nothing about uniqueness. What makes THIS pair illegal is
    /// that both definitions externalize one name — §8.3.2.2: "Except for method-names and property-names,
    /// when two or more source elements identify something with the same externalized name, they refer to the
    /// same instance" — the same sentence two outermost PROGRAM definitions collide under, so one check
    /// (COBOLNET2213) reports both. <see cref="DuplicateFunctionWord_UnderDifferentAsLiterals_1508"/> is what
    /// keeps 1508 exercised on the clash §8.4.6.7 genuinely owns.</summary>
    [Fact]
    public void DuplicateFunctionId_2213()
    {
        string src = Group("UDFT11", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).") + """

            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                GOBACK.
            END FUNCTION UDFDBL.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2213");
    }

    /// <summary>The clash §8.4.6.7 owns and §8.3.2.2 does not see: two function DEFINITIONS sharing the
    /// declared WORD while externalizing DIFFERENT names. "A user-function-name may be referenced in the
    /// REPOSITORY paragraph of any source element that follows that function definition within the compilation
    /// group" — so the word itself has to resolve, and two definitions under it cannot (kb/Work PB660). This is
    /// the complement of <see cref="DuplicateFunctionId_2213"/>: neither check subsumes the other, and a
    /// landing that folded 1508 away entirely would have dropped this case in silence.</summary>
    [Fact]
    public void DuplicateFunctionWord_UnderDifferentAsLiterals_1508()
    {
        string src = Group("UDFT11B", "    COMPUTE WS-R = FUNCTION UDFDBL(WS-A).") + """

            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL AS "UDFX2".
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                GOBACK.
            END FUNCTION UDFDBL.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1508");
    }

    /// <summary>§8.4.6.6: within a function definition its OWN user-function-name is referable with NO
    /// repository declaration — self-recursion binds (a present self-entry would be ignored, §12.3.8 GR11).
    /// The runtime behavior (5! = 120 through five nested activations) is the udf_recursion golden.</summary>
    [Fact]
    public void SelfRecursion_WithoutRepositoryEntry_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UFCT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-N PIC 9(4).
            01 L-R PIC 9(8).
            PROCEDURE DIVISION USING L-N RETURNING L-R.
            P.
                IF L-N < 2
                    MOVE 1 TO L-R
                ELSE
                    COMPUTE L-R = L-N * FUNCTION UFCT(L-N - 1)
                END-IF.
                GOBACK.
            END FUNCTION UFCT.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT12.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UFCT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(8).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UFCT(5).
                STOP RUN.
            END PROGRAM UDFT12.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.4 GR1: the container's configuration-section entries apply to each contained source
    /// unit (§12.3.3 SR1 — a contained program cannot declare its own), so a contained program's function
    /// reference resolves through the OUTER program's REPOSITORY.</summary>
    [Fact]
    public void ContainedProgram_InheritsRepositoryFunctions()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFDBL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                GOBACK.
            END FUNCTION UDFDBL.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT13.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFDBL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                CALL "UDFT13IN".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT13IN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-Q PIC 9(4).
            PROCEDURE DIVISION.
            M2.
                COMPUTE WS-Q = FUNCTION UDFDBL(3).
                EXIT PROGRAM.
            END PROGRAM UDFT13IN.
            END PROGRAM UDFT13.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>EXIT FUNCTION placement (the 0827 EXIT-family band): the pre-2023 function-return synonym
    /// may appear only in a function definition — in a program procedure division it is rejected.</summary>
    [Fact]
    public void ExitFunction_OutsideFunction_0827()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT15", "    EXIT FUNCTION."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0827");
    }

    /// <summary>The exit-function-window removal edge (Annex E.2 :49036): valid at 2002/2014, 0902 at 2023
    /// strict (the matrix row's RemovedMatrix theory proves the permissive migration contract). The runtime
    /// early-return behavior is the udf_exit_function golden (X=0014).</summary>
    [Fact]
    public void ExitFunction_Window()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFT16F.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * 2.
                EXIT FUNCTION.
            END FUNCTION UDFT16F.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT16.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFT16F.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UDFT16F(3).
                STOP RUN.
            END PROGRAM UDFT16.
            """;
        var (ok02, e02, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok02, string.Join("\n", e02));
        var (ok23, e23, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok23, "EXIT FUNCTION was removed by 2023 (Annex E.2) — strict must reject");
        EditionHarness.AssertHasDiagnostic(e23, "COBOLNET0902");
    }

    /// <summary>The RETURNING category channel (§8.4.3.2.4 GR1 / §14.2.2 SR5 — NO category restriction on a
    /// function's RETURNING item): EVERY category compiles end-to-end — elementary fixed-point numeric,
    /// alphanumeric, numeric-edited, national, FLOAT, BOOLEAN, INDEX, data POINTER, and groups of every shape
    /// (character image, binary leaf, internal REDEFINES, OCCURS DEPENDING, strongly typed). The runtime
    /// behavior is the udf_returning_categories and udf_returning_every_category goldens; this theory pins that no
    /// category is refused (kb/Work PB1419 deleted the per-shape COBOLNET1510 residue list this test used to pin
    /// GREEN — a green test holding a gap open). <paramref name="consumer"/> is the one statement that reads the
    /// result in the way its class admits (DISPLAY is not a reader of a pointer or an index).</summary>
    [Theory]
    [InlineData("ALN", "01 L-R PIC X(4).", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("GRP", "01 L-R.\n               05 L-R-A PIC 9(2).\n               05 L-R-B PIC 9(2).", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("EDT", "01 L-R PIC ZZ9.", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("NAT", "01 L-R PIC N(3).", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("FLT", "01 L-R USAGE FLOAT-LONG.", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("BOL", "01 L-R PIC 1(4).", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("BIN", "01 L-R.\n               05 L-R-A PIC X(2).\n               05 L-R-B PIC 9(4) USAGE BINARY.", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("PTR", "01 L-R USAGE POINTER.", "IF FUNCTION UDFXTAGP10UR(1) = NULL DISPLAY \"N\"")]
    [InlineData("IDX", "01 L-R USAGE INDEX.", "IF FUNCTION UDFXTAGP10UR(1) = WS-IX DISPLAY \"N\"")]
    [InlineData("RED", "01 L-R.\n               05 L-R-A PIC X(4).\n               05 L-R-B REDEFINES L-R-A PIC 9(4).", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("ODO", "01 L-R.\n               05 L-R-N PIC 9.\n               05 L-R-T PIC X OCCURS 1 TO 5 DEPENDING ON L-R-N.", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    [InlineData("STR", "01 L-T TYPEDEF STRONG.\n               05 L-T-A PIC 9(2).\n            01 L-R TYPE L-T.", "DISPLAY FUNCTION UDFXTAGP10UR(1)")]
    public void ReturningCategories_AreAllCarried(string tag, string returningDecl, string consumer)
    {
        // DISPLAY is the category-neutral reference site (a MOVE receiver would entangle Table-16 legality —
        // e.g. national→alphanumeric is a "No" cell); unique PROGRAM-IDs per fact (P10UR wave).
        string pid = $"UDFT14{tag}P10UR";
        string read = consumer.Replace("TAG", tag);
        string src = $$"""
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFX{{tag}}P10UR.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            {{returningDecl}}
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                GOBACK.
            END FUNCTION UDFX{{tag}}P10UR.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {{pid}}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFX{{tag}}P10UR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-D PIC X(1).
            01 WS-IX USAGE INDEX.
            PROCEDURE DIVISION.
            MAIN.
                {{read}}.
                STOP RUN.
            END PROGRAM {{pid}}.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, $"{tag}: " + string.Join("\n", errors));
    }

    /// <summary>The BIN row's DISCRIMINATING evidence (kb/Work PB199): a group RETURNING item with a
    /// <c>PIC 9(4) USAGE BINARY</c> leaf does not merely COMPILE — both leaves carry their VALUES across the
    /// activation boundary. §14.2.2 SR5 constrains a RETURNING item to a level-01/77 linkage entry without
    /// BASED/REDEFINES and imposes NO usage or category restriction; §8.4.3.2.4 GR1 clones that description
    /// into the caller's temp, so the group's whole-image round trip (AsImage/FromImage over the leaf's pinned
    /// bytes — V59) must reproduce the alphanumeric leaf byte-for-byte AND the binary leaf's computed value in
    /// its zoned reading form. A compile-only assertion cannot tell a real carry from a silently empty one.</summary>
    [Fact]
    public void ReturningGroup_BinaryLeaf_CarriesValuesAcrossTheActivation()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFXBINCARRYP199.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R.
               05 L-R-A PIC X(2).
               05 L-R-B PIC 9(4) USAGE BINARY.
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                MOVE "AB" TO L-R-A.
                COMPUTE L-R-B = L-X * 2.
                GOBACK.
            END FUNCTION UDFXBINCARRYP199.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT14BINCARRYP199.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFXBINCARRYP199.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-G.
               05 WS-A PIC X(2).
               05 WS-B PIC 9(4) USAGE BINARY.
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION UDFXBINCARRYP199(1) TO WS-G.
                DISPLAY "A=[" WS-A "] B=[" WS-B "]".
                STOP RUN.
            END PROGRAM UDFT14BINCARRYP199.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2002);
        Assert.True(ok, detail);
        Assert.Equal(CutRunner.Normalize("A=[AB] B=[0002]"), stdout);
    }

    /// <summary>Shapes the goldens do not cover, locked at bind level: a no-argument function, a nested
    /// user-function argument (activation order = registration order, §8.4.3.2.4 GR2), a user-function
    /// argument inside an INTRINSIC call, and an IF-condition reference (once-per-execution — exact).</summary>
    [Theory]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(FUNCTION UDFDBL(WS-A)).")]
    [InlineData("    COMPUTE WS-R = FUNCTION MOD(FUNCTION UDFDBL(3), 4).")]
    [InlineData("    COMPUTE WS-R = FUNCTION UDFDBL(FUNCTION MOD(7, 4)).")]
    [InlineData("    IF FUNCTION UDFDBL(WS-A) = 8 DISPLAY \"Y\" END-IF.")]
    [InlineData("    IF FUNCTION UDFDBL(WS-A) = 8 AND WS-A = 4 DISPLAY \"Y\" END-IF.")]
    [InlineData("    MOVE FUNCTION UDFDBL(WS-A) TO WS-R.")]
    public void SupportedShapes_Bind(string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Group("UDFT7", body), 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>No-argument function: <c>FUNCTION name</c> with no subscript part, zero USING formals.</summary>
    [Fact]
    public void NoArgFunction_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UDFK.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-R PIC 9(4).
            PROCEDURE DIVISION RETURNING L-R.
            P.
                MOVE 7 TO L-R.
                GOBACK.
            END FUNCTION UDFK.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT8.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UDFK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UDFK.
                STOP RUN.
            END PROGRAM UDFT8.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.8.2 GR12 (:14885): a REPOSITORY-declared function-prototype-name refers to the
    /// USER-DEFINED function "and not to an intrinsic function of the same name" — the spec's own
    /// factorial-override pattern (:43651). A user function named SQRT must bind against the FUNCTION-ID
    /// unit's ONE formal, not the intrinsic catalog's signature.</summary>
    [Fact]
    public void UserFunctionShadowsIntrinsic_Gr12()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. SQRT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-B PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
            P.
                COMPUTE L-R = L-A + L-B.
                GOBACK.
            END FUNCTION SQRT.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT9.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION SQRT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION SQRT(4, 5).
                STOP RUN.
            END PROGRAM UDFT9.
            """;
        // Two arguments: the INTRINSIC SQRT takes exactly one — binding proves the user function won.
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    // ── M2-UDF-3: function prototypes (ISO §11.5 Format 2 / §10.6 / §12.3.8; DEVLOG 624) ─────────────────────

    /// <summary>UDF-3: a FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) preceding the caller (§10.6.2 SR1) supplies
    /// the signature; the same-name in-group DEFINITION that follows is authoritative (§12.3.8 GR11a) — the pair
    /// is NOT a COBOLNET1508 duplicate. Binds clean at 2002.</summary>
    [Fact]
    public void Prototype_WithDefinition_NotDuplicate_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPSQ IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPSQ.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT17.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UPSQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4) VALUE 6.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UPSQ(WS-A).
                STOP RUN.
            END PROGRAM UDFT17.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPSQ.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                COMPUTE L-R = L-X * L-X.
                GOBACK.
            END FUNCTION UPSQ.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>UDF-3: a prototype-declared function with NO in-group definition BINDS — the prototype supplies
    /// the §8.4.3.2.4 GR1 signature and the definition is a separately-compiled target resolved at run time
    /// (§12.3.8 GR11c / §8.4.3.2.4 GR6b). The former COBOLNET1505 "not implemented" gap is closed for the
    /// prototyped case (the cross-assembly run is proven in InterProgramFileDifferentialTests).</summary>
    [Fact]
    public void PrototypeOnly_NoInGroupDefinition_Binds()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPXTERN IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPXTERN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT18.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION UPXTERN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4) VALUE 6.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE WS-R = FUNCTION UPXTERN(WS-A).
                STOP RUN.
            END PROGRAM UDFT18.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, "a prototype-declared function must bind with no in-group definition: " + string.Join("\n", errors));
    }

    /// <summary>UDF-3: §10.6.2 SR3 — an in-group prototype and same-name definition shall have the same
    /// signature; an argument-count mismatch is COBOLNET1513.</summary>
    [Fact]
    public void PrototypeDefinitionSignatureMismatch_1513()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPMIS IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UPMIS.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT19.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT19.
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPMIS.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-A PIC 9(4).
            01 L-B PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
            P.
                COMPUTE L-R = L-A + L-B.
                GOBACK.
            END FUNCTION UPMIS.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1513");
    }

    /// <summary>UDF-3: a function prototype without a RETURNING phrase is ill-formed — COBOLNET1507 (a function
    /// cannot deliver a result without it, §14.2).</summary>
    [Fact]
    public void PrototypeWithoutReturning_1507()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UPNORET IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            PROCEDURE DIVISION USING L-X.
            END FUNCTION UPNORET.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT20.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT20.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1507");
    }

    /// <summary>UDF-3: FUNCTION-ID … IS PROTOTYPE is a 2002 introduction — a prototype at 85 is rejected with the
    /// edition-naming COBOLNET0900 (the W1.5 ReservedWordEditionHints mapping for the {is2002()}?-gated tail).</summary>
    [Fact]
    public void PrototypeAt85_0900()
    {
        string src = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. UP85 IS PROTOTYPE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(4).
            01 L-R PIC 9(6).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            END FUNCTION UP85.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDFT21.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            END PROGRAM UDFT21.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 85);
        Assert.False(ok, "a function prototype is 2002+; 85 must reject");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
    }

    // ── M2-UDF-4: REPOSITORY specifiers + the §8.4.3.2 SR2 keyword-omitted reference form (DEVLOG 626) ────────

    /// <summary>A minimal program that references <paramref name="expr"/> as its COMPUTE/MOVE body under the
    /// given <paramref name="repository"/> and optional extra <paramref name="ws"/> — for the keyword-omission
    /// facts (byte-exact behaviour rides the udf_keyword_omitted golden).</summary>
    private static string KoGroup(string pid, string repository, string body, string ws = "") => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{pid}}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
        {{repository}}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 12.
        01 WS-B PIC 9(4) VALUE 34.
        01 WS-R PIC 9(4).
        {{ws}}
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
            STOP RUN.
        END PROGRAM {{pid}}.
        """;

    /// <summary>§8.4.3.2 SR2 + §12.3.8 GR13/GR14 — with FUNCTION ALL INTRINSIC in the REPOSITORY the word
    /// FUNCTION may be omitted for an intrinsic reference (SR6: the '(' is the argument list). Binds at 2002.</summary>
    [Fact]
    public void KeywordOmitted_AllIntrinsic_Binds()
    {
        string src = KoGroup("UDFT22", "    FUNCTION ALL INTRINSIC.",
            "    COMPUTE WS-R = MAX(WS-A, WS-B).\n    MOVE MIN(WS-A, WS-B) TO WS-R.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>§12.3.8 — a specifically-named intrinsic (FUNCTION MAX INTRINSIC, no ALL) also enables keyword
    /// omission for THAT name.</summary>
    [Fact]
    public void KeywordOmitted_NamedIntrinsic_Binds()
    {
        string src = KoGroup("UDFT23", "    FUNCTION MAX INTRINSIC.", "    COMPUTE WS-R = MAX(WS-A, WS-B).");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>ISO §8.3.2.1 rule 5 (kb/Work PB65): under FUNCTION ALL INTRINSIC every intrinsic-function-name is
    /// "identified in a function-specifier in the REPOSITORY paragraph", so a data item named MOD is an ILLEGAL
    /// user-defined word — COBOLNET1649 at the declaration. (The previous form of this test pinned "the data item
    /// wins" as green while its own remark conceded "enforcing it is staged" — the green-test-holds-a-gap-open
    /// shape; the standard's prohibition is what makes the FUNCTION-less reference unambiguous.)</summary>
    [Fact]
    public void KeywordOmitted_RepositoryIntrinsicName_CannotBeAUserWord()
    {
        string src = KoGroup("UDFT24", "    FUNCTION ALL INTRINSIC.", "    MOVE MOD(2) TO WS-R.",
            ws: "01 GRP.\n           05 MOD PIC 9(4) OCCURS 3 VALUE 7.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "a data item spelling a REPOSITORY-identified intrinsic-function-name shall be rejected (ISO §8.3.2.1 rule 5)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1649");
    }

    /// <summary>The rule-5 CONTROL: without a REPOSITORY entry for MOD an intrinsic-function-name IS a legal
    /// user-defined word (§8.3.2.1 rule 5's permission), and the FUNCTION-less spelling `MOD(2)` is the subscripted
    /// data item — never mis-routed to the intrinsic (§8.4.3.2.3 SR2 requires the word FUNCTION there).</summary>
    [Fact]
    public void KeywordOmitted_UndeclaredIntrinsicName_StaysADataItem()
    {
        string src = KoGroup("UDFT24B", "    FUNCTION UDFDBL.", "    MOVE MOD(2) TO WS-R.",
            ws: "01 GRP.\n           05 MOD PIC 9(4) OCCURS 3 VALUE 7.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.True(ok, "a data item named like an un-declared intrinsic must stay a subscript: " + string.Join("\n", errors));
    }

    /// <summary>§8.4.3.2.3 SR2: WITHOUT a REPOSITORY intrinsic/ALL specifier the word FUNCTION is REQUIRED — a
    /// bare MAX(a, b) is NOT the intrinsic. Since kb/Work R22 this is a COMPILE-TIME rejection: the name matches
    /// a catalogued intrinsic, no data item shadows it, and the REPOSITORY does not declare it, so the reference
    /// draws COBOLNET1543. (The previous form of this test PINNED the run-time loud-fail as green — the
    /// green-test-holds-a-gap-open shape: its doc claimed "a compile-time assertion cannot distinguish it from
    /// the intrinsic-routed case", which R22 made false.) Contrast: with FUNCTION ALL INTRINSIC the same source
    /// runs and computes MAX=34.</summary>
    [Fact]
    public void KeywordOmitted_RequiresRepositoryDeclaration_CompileTimeReject()
    {
        string src = KoGroup("UDFKON", "    FUNCTION UDFDBL.",
            "    COMPUTE WS-R = MAX(WS-A, WS-B).\n    DISPLAY \"R=\" WS-R.");
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "MAX without FUNCTION and without an ALL/named-intrinsic specifier is not the "
            + "intrinsic and shall be rejected at compile time (ISO §8.4.3.2.3 SR2 — kb/Work R22)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1543");
    }
}
