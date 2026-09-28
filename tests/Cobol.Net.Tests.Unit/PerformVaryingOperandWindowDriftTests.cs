// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB437 — the EVALUATION WINDOW of every PERFORM VARYING operand slot, pinned per slot.
///
/// <para><b>The rule.</b> ISO §14.9.28.4 GR12 states it as one sentence: "Item identification for identifier-3,
/// identifier-4, identifier-6, identifier-7, index-name-2, and index-name-4 is done each time the content of the
/// data item referenced by the identifier or the index referenced by the index-name is used in a SETTING or
/// AUGMENTING operation." So every FROM and every BY is re-read at each such operation — and a FUNCTION
/// reference in one activates at each such operation (§8.4.3.2.4 GR1/GR6a: the value "is determined when the
/// function is referenced at runtime"). The FIRST level's FROM is the one slot whose setting operation happens
/// exactly ONCE per execution of the statement (GR13 a) for TEST BEFORE, GR13 b) for TEST AFTER, both of which
/// initialize it before any transfer of control and never again), which is the only window a statement-scoped
/// hoist is EXACT for.
///
/// <para><b>Why a drift test and not only the goldens.</b> The two per-evaluation slots are covered by runtime
/// cardinality in <c>tests/conformance/2023/pb437_varying_operand_windows.cob</c>, but the FIRST-level FROM's
/// window is not observable from a program's output at all: GR13 uses that operand once whichever window it
/// carries, so only the activation COUNT differs, and only a side-effecting function could see it. The window
/// table is therefore pinned HERE, over the generated C#, where "before the loop" and "inside the loop" are
/// directly measurable. Before PB437 the two per-evaluation slots had no window — they had a REFUSAL
/// (COBOLNET1509, on source the standard admits).</para>
///
/// <para><b>What is measured.</b> Each program carries exactly ONE function reference, in one operand slot,
/// written as a table SUBSCRIPT so it registers a §15.4 temporary store as a pre-op — the same pending list a
/// user-function activation uses, because §8.8.4.13 r2 governs "functions" and not one spelling of them. The
/// generated C# is then asked where that store landed relative to the varying phrase's own loop header.</para>
/// </summary>
public sealed class PerformVaryingOperandWindowDriftTests
{
    /// <summary>The §15.4 temporary store a function-bearing subscript emits. <c>FUNCTION INTEGER</c> lowers to
    /// the floor intrinsic (ISO §15.36 — "the greatest integer ≤ argument-1"), and each program below writes
    /// exactly one function reference, so every occurrence of this text is one activation of it.</summary>
    private const string Activation = "CobolIntrinsics.Floor(";

    /// <summary>The varying phrase's own loop header. NOT plain <c>"while ("</c>: the paragraph dispatcher's
    /// <c>while ((uint)__pc &lt; (uint)__N)</c> encloses every statement, so that spelling would measure
    /// against the dispatcher and every assertion below would pass for the wrong reason.</summary>
    private const string Loop = "while (!(";

    private static string Program(string pid, string perform) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-G.
           05 W-E PIC 9(2) OCCURS 5 TIMES.
        01 K PIC 9(2) VALUE 1.
        01 I PIC 9(4).
        01 J PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
        {perform}
            STOP RUN.
        """;

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pvwindow_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2002)));
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    /// <summary>Every activation's position, and the varying loop's header position, in the generated C#.</summary>
    private static (int Loop, List<int> Acts, string Cs) Measure(string pid, string perform)
    {
        string cs = Emit(Program(pid, perform));
        int loop = cs.IndexOf(Loop, StringComparison.Ordinal);
        Assert.True(loop >= 0, "the varying phrase did not lower to a loop:\n" + cs);
        var acts = new List<int>();
        for (int i = cs.IndexOf(Activation, StringComparison.Ordinal); i >= 0;
             i = cs.IndexOf(Activation, i + 1, StringComparison.Ordinal))
            acts.Add(i);
        Assert.True(acts.Count > 0,
            $"no function activation ('{Activation}') in the generated source — the operand never bound:\n{cs}");
        return (loop, acts, cs);
    }

    /// <summary>The VARYING level's own FROM — identifier-3 / index-name-2 / literal-1. GR13 a)/b) set it ONCE
    /// per execution of the statement, so exactly ONE activation is emitted, BEFORE the loop.
    /// <para>⚠ THIS ARM IS A CARDINALITY ASSERTION, NOT A DISCRIMINATOR BETWEEN THE TWO MECHANISMS, and saying
    /// so is the honest statement of what it measures. Measured: flipping <c>IsPerEvaluationWindow</c> to true
    /// for this slot leaves the generated source UNCHANGED — the statement hoist and the site carrier coincide
    /// here, which is precisely what "the hoist is EXACT for the first-level FROM"
    /// (<c>UdfBinder.UdfWrapCalls</c>) means. What this arm does catch is the failure that matters: a SECOND
    /// activation (double-evaluation), or one that drifted INSIDE the loop. The two arms below are the
    /// discriminating ones — flipping the table to statement-scope makes both fail.</para></summary>
    [Fact]
    public void FirstLevelFrom_HoistsToStatementScope()
    {
        var (loop, acts, cs) = Measure("PVWINA",
            "    PERFORM VARYING I FROM W-E (FUNCTION INTEGER(K)) BY 1 UNTIL I > 3\n"
            + "        CONTINUE\n"
            + "    END-PERFORM.");
        Assert.True(acts.Count == 1 && acts[0] < loop,
            "the FIRST-level FROM operand is set once per execution of the statement (ISO §14.9.28.4 GR13 a)/b)), "
            + "so its function activation belongs on the statement hoist, once, BEFORE the loop header:\n" + cs);
    }

    /// <summary>A BY operand — identifier-4 / literal-2. GR12 reads it at every AUGMENT, so its activations ride
    /// the per-evaluation carrier and are emitted INSIDE the loop, at the augment. Exactly one site: the phrase
    /// has one augment.</summary>
    [Fact]
    public void ByOperand_EvaluatesPerAugment()
    {
        var (loop, acts, cs) = Measure("PVWINB",
            "    PERFORM VARYING I FROM 1 BY W-E (FUNCTION INTEGER(K)) UNTIL I > 3\n"
            + "        CONTINUE\n"
            + "    END-PERFORM.");
        Assert.True(acts.Count == 1 && acts[0] > loop,
            "a BY operand is read at every augmenting operation (ISO §14.9.28.4 GR12), so its function activation "
            + "belongs INSIDE the loop. Before kb/Work PB437 this program was REJECTED (COBOLNET1509):\n" + cs);
    }

    /// <summary>An AFTER level's FROM — identifier-6 / index-name-4 / literal-3. It is SET TWICE by two different
    /// sub-rules, and both are setting operations under GR12, so the activation appears at BOTH: GR13 a)'s
    /// one-shot left-to-right initialization before the outermost test (outside the loop), and GR13 e) 2 a.'s
    /// re-initialization when the inner condition goes true (inside the outer loop, before the outer augment).
    /// Two sites is the rule, not an artifact — a single site would mean one of the two settings read a stale
    /// value.</summary>
    [Fact]
    public void AfterLevelFrom_EvaluatesAtEverySetting()
    {
        var (loop, acts, cs) = Measure("PVWINC",
            "    PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3\n"
            + "            AFTER J FROM W-E (FUNCTION INTEGER(K)) BY 1 UNTIL J > 3\n"
            + "        CONTINUE\n"
            + "    END-PERFORM.");
        Assert.True(acts.Count == 2 && acts[0] < loop && acts[1] > loop,
            "an AFTER level's FROM operand is read at GR13 a)'s initialization (before the loop) AND at "
            + "GR13 e) 2 a.'s re-initialization (inside the outer loop) — both are setting operations under ISO "
            + "§14.9.28.4 GR12. Before kb/Work PB437 this program was REJECTED (COBOLNET1509):\n" + cs);
    }
}
