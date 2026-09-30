// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The ALTER / alterable-GO-TO / SET-external-switch emitter (P7 Step 9c — a real collaborator over
/// the per-unit <see cref="EmitContext"/>, extracted from the CSharpEmitter.AlterSwitches partial).</summary>
internal sealed class AlterSwitchEmitter(EmitContext ctx, DispatchState dispatch)
{
    // ── ALTER + the alterable GO TO (ANSI X3.23-1985; COBOLNET_CONTROL_FLOW_DESIGN D4) ───────────────────────

    /// <summary>Declare one mutable <c>private static int _alter_&lt;para&gt; = &lt;defaultPc&gt;;</c> per altered
    /// paragraph (D4 — the C#-native replacement for the legacy slot table), collected from every
    /// <see cref="BoundGoToAlterable"/> in the bound program. Called from the dispatcher emission BEFORE the
    /// paragraph bodies render, so the dispatcher cases can reference the fields. Default = the written GO TO
    /// target's pc, or −1 for a never-written target-less <c>GO TO.</c> (undefined until ALTERed; −1 exits the
    /// <c>while ((uint)__pc &lt; (uint)__N)</c> dispatcher loop — D4's run-unit-end realization).</summary>
    public void EmitFields(BoundProgram bound, CodeWriter w)
    {
        var fields = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var para in bound.Paragraphs)
            foreach (var s in para.Statements)
                CollectFields(s, fields);
        foreach (var (field, defaultPc) in fields)
            w.Line($"private int {field} = {defaultPc};   // ALTERable GO TO target pc (control-flow design D4; instance — ALTER state is per-program-instance, ISO §14.6.2.3.2 resets it via a fresh instance)");
    }

    /// <summary>Collect alterable-GO-TO fields from a statement and every nested statement — recursing over the
    /// generated <see cref="BoundStatementTree.StatementChildren"/> (PHASE-07 Step 6g), the ONE drift-proof
    /// enumeration of every nesting container the binder produces, so a new container node is covered automatically.
    /// A missed container would reference an undeclared field and fail the generated-C# compile LOUDLY, never run
    /// silently wrong. (Replaces the former hand-maintained walker, which had missed SEQUENCE/CALL/WRITE/keyed/RETURN
    /// phrase bodies.)</summary>
    private static void CollectFields(BoundStatement s, Dictionary<string, int> fields)
    {
        if (s is BoundGoToAlterable g) fields.TryAdd(g.AlterField, g.DefaultPc);
        foreach (var child in s.StatementChildren())
            CollectFields(child, fields);
    }

    /// <summary>The alterable GO TO transfers to the CURRENT field value (D4:
    /// <c>__pc = _alter_X; goto __xfer;</c>) — the written target until an ALTER executes, then the most recent
    /// ALTER's destination; −1 (a never-ALTERed target-less GO TO, undefined per ANSI-85) exits the dispatcher
    /// loop. Like every transfer out of a paragraph it leaves through <see cref="DispatchState.TransferOut(string,string)"/>,
    /// never a bare <c>break</c> a lowered container could capture (kb/Work PB405).</summary>
    public void EmitGoTo(BoundGoToAlterable g)
    {
        var w = ctx.Writer;
        // X3.23-1985 USE FOR DEBUGGING (VCR 7.17): an altered GO TO transfer is DEBUG-CONTENTS SPACES (Transfer),
        // DEBUG-LINE the GO TO statement's own line.
        dispatch.EmitDebugCause(w, "Transfer", g.SourceLine);
        w.Line(dispatch.ExplicitTransferOut(g.AlterField));
    }

    /// <summary>ALTER assigns each entry's new destination pc into the target paragraph's field at the ALTER site
    /// (ANSI X3.23-1985 ALTER GR — subsequent executions of the GO TO transfer to proc-2).</summary>
    public void EmitAlter(BoundAlter al)
    {
        foreach (var e in al.Entries)
            ctx.Writer.Line($"{e.AlterField} = {e.NewPc};");
    }

    // ── SET Format 3 — external switches (ISO §14.9.39 F3 GR5; §12.3.7 GR3) ──────────────────────────────────

    /// <summary>Each (switch, position) pair assigns the run-unit switch store in source order — the switch is
    /// modified so its associated condition-names evaluate per the ON/OFF phrase (GR5).</summary>
    public void EmitSetSwitches(BoundSetSwitches s)
    {
        foreach (var (name, on) in s.Switches)
            ctx.Writer.Line($"ExternalSwitches.Set({CsLiteral(name)}, {(on ? "true" : "false")});");
    }
}
