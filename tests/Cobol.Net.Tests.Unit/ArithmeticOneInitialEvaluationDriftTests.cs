// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN ARITHMETIC STATEMENT EVALUATES ITS RIGHT-HAND SIDE ONCE, AND THE §14.7.4.3 r7 GATE TESTS THAT ONE
/// INTERMEDIATE (kb/Work PB654). <c>ArithmeticEmitter.StoreArith</c> wrote <c>value.Expr</c> into the
/// PROHIBITED representability test and AGAIN into the store that follows. ISO §14.7.7 rule 4 a) — "The
/// initial evaluation of the statement is done and the result of this operation is placed in an intermediate
/// data item" — and rule 4 b) — "the intermediate data item is stored in or combined with and then stored in
/// each single resulting data item" — admit exactly one evaluation, and rule 7's test is a test on THAT
/// intermediate.
/// <para>The cost was a wrong answer, not merely a wasted multiply: for a function whose successive references
/// are not the same value (§15.75.3 rule 5 — "In each case, subsequent references without specifying
/// argument-1 return the next number in the current sequence") the test examined one returned value and the
/// store landed the NEXT one, and the statement consumed two elements of the pseudo-random sequence.</para>
/// <para>This class reads the EMITTED C# because that is where the defect lives: the corpus golden
/// <c>tests/conformance/2014/pb654_prohibited_gate_one_evaluation</c> pins the BEHAVIOUR through RANDOM's
/// sequence position, and these pin the SHAPE over every receiver category and phrase combination, so a new
/// arm that re-spells the value fails here even if no golden happens to cross it.</para>
/// </summary>
public sealed class ArithmeticOneInitialEvaluationDriftTests
{
    /// <summary>Six arithmetic stores, one <c>FUNCTION RANDOM</c> reference each, across every receiver
    /// category and phrase combination that reaches <c>StoreArith</c> with a float value: plain numeric and
    /// numeric-EDITED under ON SIZE ERROR + PROHIBITED (the two arms the r7 gate must cover), a non-PROHIBITED
    /// mode under ON SIZE ERROR, PROHIBITED with no phrase at all (no gate — §14.7.4.3 r7's condition is the
    /// size error condition, which without a phrase or EC-SIZE checking has its own disposition), a bare
    /// no-phrase store, and a FLOAT receiver (no scaled store at all).</summary>
    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB654DRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 R   PIC 9V9(9).
        01 ED  PIC ZZ9.999.
        01 C2  USAGE COMP-2.
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE C2 = 1.
            COMPUTE R ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE ED ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE R ROUNDED MODE IS NEAREST-EVEN = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE R ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM.
            COMPUTE R = C2 * FUNCTION RANDOM.
            COMPUTE C2 = C2 * FUNCTION RANDOM.
            STOP RUN.
        """;

    /// <summary>The number of <c>FUNCTION RANDOM</c> references written in <see cref="Program"/>.</summary>
    private const int SourceReferences = 6;

    private static string EmitCSharp(string source, int edition)
    {
        string dir = Directory.CreateTempSubdirectory("pb654drift").FullName;
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: edition, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }

    /// <summary>
    /// INVARIANT 1 — THE EMITTED C# SPELLS EACH RIGHT-HAND SIDE EXACTLY AS OFTEN AS THE SOURCE WROTE IT.
    /// A second spelling IS a second evaluation (§14.7.7 rule 4 a)), and for <c>FUNCTION RANDOM</c> a second
    /// evaluation is a different value. Counting the CALL TEXT rather than reasoning about the branch shape is
    /// deliberate: it catches the defect wherever a future arm reintroduces it.
    /// </summary>
    [Fact]
    public void EachRightHandSide_IsSpelledOncePerSourceReference()
    {
        string cs = EmitCSharp(Program, 2014);
        int emitted = Regex.Matches(cs, @"CobolIntrinsics\.Random\(\)").Count;
        Assert.Equal(SourceReferences, emitted);
    }

    /// <summary>
    /// INVARIANT 2 — AND THE r7 GATE READS THAT INTERMEDIATE, NEVER A RE-SPELLED EXPRESSION. The gate's first
    /// argument must be the <c>__ie</c> local <c>ArithmeticEmitter.Snapshot</c> introduced, which is rule 4 a)'s
    /// "intermediate data item" made literal. Stated separately from invariant 1 because it is the property that
    /// survives a change of function: a gate over any impure right-hand side is wrong the same way, and this
    /// assertion does not depend on RANDOM being the one that shows it.
    /// </summary>
    [Fact]
    public void TheProhibitedGate_TestsTheMaterializedIntermediate()
    {
        string cs = EmitCSharp(Program, 2014);
        var gates = Regex.Matches(cs, @"CobolFloat\.InexactAtScale\(([^,]+),");
        Assert.NotEmpty(gates);
        foreach (Match g in gates)
            Assert.Matches(@"^__ie\d+$", g.Groups[1].Value.Trim());
    }

    /// <summary>
    /// INVARIANT 3 — THE GATE COVERS EVERY FIXED-SCALE RECEIVER CATEGORY, NOT JUST THE PLAIN NUMERIC ONE.
    /// §14.7.4.3 rule 7 speaks of "the resultant identifier" with no category qualification, and
    /// <c>CobolFloat.ToScaled</c> lands a PROHIBITED transfer TRUNCATED for every one of them — so an ungated
    /// arm stores the truncated value and raises nothing. The numeric-EDITED and LOCALE-edited arms were
    /// exactly that for three waves (measured: SQRT(3) into <c>PIC ZZ9.999</c> stored 1.732, no size error).
    /// <see cref="Program"/> writes ONE gated store per covered category, so the count is the proof.
    /// </summary>
    [Fact]
    public void TheProhibitedGate_IsEmittedForBothTheNumericAndTheEditedReceiver()
    {
        string cs = EmitCSharp(Program, 2014);
        Assert.Equal(2, Regex.Matches(cs, @"CobolFloat\.InexactAtScale\(").Count);
    }
}
