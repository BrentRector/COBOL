// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The INITIALIZE verb emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name).</summary>
internal sealed class InitializeEmitter(EmitContext ctx, MoveEmitter move)
{
    /// <summary>INITIALIZE (ISO §14.9.20) — render the bind-time expansion: each <see cref="InitializeStore"/> IS
    /// the spec's implicit elementary MOVE (GR4), emitted through the ONE MOVE store path (<c>EmitMove</c> →
    /// <c>ConvertSource</c> — so REPLACING/VALUE/default senders get identical conversion, editing, padding and
    /// truncation to an explicit MOVE; e.g. a numeric sender into a numeric-edited receiver edits, and the GR6c
    /// ZEROES default produces the EDITED zero, never spaces); each <see cref="InitializeLoop"/> is one OCCURS
    /// dimension (GR5b2 — every occurrence), nested outermost-first. Actions are already in GR3/GR8 order.</summary>
    public void Emit(BoundInitialize ini)
    {
        foreach (var action in ini.Actions) EmitAction(action);
    }

    private void EmitAction(InitializeAction action)
    {
        var w = ctx.Writer;
        switch (action)
        {
            case InitializeStore s:
                // §14.9.20.4 GR4 — an implicit MOVE, ONE code path, receiver category by receiver category:
                // "Otherwise, the implicit statement is: MOVE sending-operand TO receiving-operand." The rule
                // exempts only the five pointer-ish categories (SET, the arms below); a COMP-1/COMP-2/FLOAT-*
                // receiver is category NUMERIC (§8.5.2), so it takes exactly the path its explicit MOVE takes —
                // MoveEmitter's float-receiver arm in ConvertSource, which owns the GR6 d)4.a EC-DATA-OVERFLOW
                // check, the single/double store cast, AND the WINDOWED (image-stored, Tier-B) re-encode
                // `target.StoreAsImage ? NumFormatImageFloat(v) : v`. ⛔ There is deliberately NO float arm here:
                // one was carried until kb/Work PB420 on the premise that "the float MOVE path is deferred
                // backend-wide", a premise that stopped being true (PB271 hardened that very path) with nothing
                // to notice — one float leaf then made the WHOLE statement throw at run time.
                // <c>StaleDeferralDriftTests</c> keeps it that way.
                // ⛔ The move was BOUND (kb/Work PB880) — built here it missed every storage fact the bind collects.
                move.Emit(s.Move);
                break;
            case InitializeSetNull s:
                // §14.9.20 GR4/GR6c: an implicit SET Target TO the predefined NULL (data-pointer → ManagedPointer.Null,
                // program-pointer → ProgramPointer.Null, object-reference → null). A SET, NOT a MOVE — reuses the
                // item's DefaultInitializer, matching SetEmitter.EmitSetPointer / OoEmitter.EmitSetObjectRef.
                w.Line(PlaceRenderer.Write(s.Target, s.Target.Item.Pic!.DefaultInitializer));
                break;
            case InitializeSetFrom s:
                // §14.9.20.4 GR4 + GR6b: an implicit `SET Target TO identifier-2`, rendered as THE SET STATEMENT
                // renders it for the same operand pair — SetEmitter.EmitSetPointer's straight handle copy for the
                // pointer family (§14.9.39 Format 7), OoEmitter's Format-5 cast-and-copy for an object-reference
                // receiver (§14.9.39 GR9, "reference copy"). §14.9.20.3 SR3 has already refused literal-1 and SR4
                // the category mismatch, so the pair is a valid SET by the time it reaches here.
                w.Line(PlaceRenderer.Write(s.Target,
                        s.Target.Item.Pic is { Category: PicCategory.ObjectReference } orp
                            ? $"({orp.ClrType})({PlaceRenderer.Read(s.Source)})"
                            : PlaceRenderer.Read(s.Source))
                    + "   // INITIALIZE REPLACING — implicit SET (ISO §14.9.20.4 GR4/GR6b)");
                break;
            case InitializeLoop l:
                // ONE loop over the ONE occurrence-count model (kb/Work PB393): a fixed OCCURS count, an
                // occurs-depending table's CURRENT count under §13.18.38.4 GR8a, or a dynamic-capacity table's
                // current capacity under §14.9.20.4 GR10 ("all the elements of the table up to current capacity
                // … are initialized … and the current capacity of the table is left unchanged" — the capacity is
                // untouched because the stores go through RefReceiving WITHIN the bound, which never grows).
                using (w.Block($"for (long {l.Var} = 1; {l.Var} <= {PlaceRenderer.OccurrenceCount(l.Count)}; {l.Var}++)"))
                    foreach (var b in l.Body)
                        EmitAction(b);
                break;
            case InitializeOccurrenceSelect sel:
                // ISO §14.9.20.4 GR5c1c/GR6a3 — the only per-occurrence arm of the expansion: a Format-2 (table)
                // VALUE keys a different literal to each occurrence, and the occurrences it does not key are not
                // receiving-operands under the VALUE phrase at all. Arms are mutually exclusive by construction
                // (one per distinct literal), so the if/else-if chain is a decision, not a fall-through.
                bool first = true;
                foreach (var arm in sel.Arms)
                {
                    using (w.Block($"{(first ? "if" : "else if")} ({OccurrenceTest(sel.Occurrences, arm.When)})"))
                        EmitAction(arm.Do);
                    first = false;
                }
                if (sel.Otherwise is { } fallback)
                {
                    if (first) EmitAction(fallback);          // no arm survived — the select degenerates to its tail
                    else using (w.Block("else")) EmitAction(fallback);
                }
                break;
            case InitializeErrorAction e:
                w.Line(LoudStmt(e.Feature));
                break;
        }
    }

    /// <summary>The run-time test for one <see cref="InitializeOccurrenceArm"/>: the loop variables of the
    /// subject's OCCURS chain (most inclusive first, ISO §13.18.63.3 SR20's order) matched against each occurrence
    /// tuple the arm covers — a conjunction per tuple, disjoined over the tuples. The tuples are bind-time
    /// constants, so nothing but the loop variables is read at run time.</summary>
    private static string OccurrenceTest(IReadOnlyList<Position> occurrences, IReadOnlyList<Subscripts> tuples) =>
        string.Join(" || ", tuples.Select(t =>
        {
            string conj = string.Join(" && ", occurrences.Select((o, i) => $"{PositionRenderer.Render(o)} == {t[i]}"));
            return occurrences.Count > 1 && tuples.Count > 1 ? $"({conj})" : conj;
        }));
}
