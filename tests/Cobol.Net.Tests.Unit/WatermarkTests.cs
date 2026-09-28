// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// P6 Step 6 — the completion-phase WATERMARK gate: the construction-time DAG assert guards the declared pass
/// LIST; the watermark guards what actually RAN on a binder — reading a fact before its producing pass is a loud,
/// located error rather than a silent miscompile. ALWAYS-ON (Debug AND Release): the gate was
/// <c>[Conditional("DEBUG")]</c> at first landing and CI's RELEASE test leg stripped the call sites, failing the
/// throw-expecting tests below — the fix keeps the guard in every configuration (DEVLOG 774).
/// </summary>
public sealed class WatermarkTests
{
    [Fact]
    public void Require_BeforeProducingPassRan_Throws()
    {
        var db = new DataBinder();
        var ex = Assert.Throws<InvalidOperationException>(() => db.Require(PassPhase.OccursResolved, "test-fact"));
        Assert.Contains("test-fact", ex.Message);
        Assert.Contains("OccursResolved", ex.Message);
    }

    [Fact]
    public void Require_AfterProduced_Passes()
    {
        var db = new DataBinder();
        db.MarkProduced(PassPhase.OccursResolved);
        db.Require(PassPhase.OccursResolved, "test-fact");   // must not throw (exact)
        db.Require(PassPhase.TypesExpanded, "test-fact");    // must not throw (earlier phase)
    }

    /// <summary>The flagged late-fact read point: a <c>CapacityRegisters</c> read before <c>DynamicResolve</c>
    /// produced it trips the gate (the "read a null CapacityRegister" silent-miscompile class, now structural).</summary>
    [Fact]
    public void CapacityRegisters_ReadBeforeOccursResolved_Trips()
    {
        var db = new DataBinder();
        Assert.Throws<InvalidOperationException>(() => _ = db.CapacityRegisters.Count);
    }

    /// <summary>End-to-end: a real group bind advances every unit binder's watermark to the manifest's TERMINAL
    /// phase — the gate can never fire on a well-formed compile (the pipeline itself is the proof).</summary>
    [Fact]
    public void FullBind_AdvancesWatermarkToTerminalPhase()
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_wm_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. WMPROG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                MOVE 7 TO WS-N.
                DISPLAY WS-N.
                STOP RUN.
            """);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var comp = new CSharpEmitter().Bind(tree!, new EditionContext(2023));
            Assert.All(comp.Units, u => Assert.Equal(PassPhase.EditionConformanceChecked, u.Data.Watermark));
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
