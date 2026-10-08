// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using ClassKind = CobolNet.Binding.CobolClass;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The INSPECT verb emitter (P7 Step 9d — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name). Every
/// runtime-member fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class InspectEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith)
{
    /// <summary>
    /// INSPECT (ISO §14.9.22) → one image snapshot + runtime cycle calls. Identifier-1's character image is read
    /// ONCE (GR6 item identification; GR4 — an edited/unsigned-numeric DISPLAY item inspects as its redefined
    /// character image, GR4d — a signed numeric item inspects de-signed); a format 3 then runs the tallying pass
    /// FOLLOWED by the replacing pass over that same image (GR19 — two successive statements; tallying never
    /// writes identifier-1 UNLESS a counter shares its storage — GR13's undefined overlap, which docs/CONFORMANCE.md
    /// D-INS1/D-INS2 define as: each later tallying operand, and the replacing pass, read identifier-1 and the
    /// operand values again after the counters stored so far — so the one snapshot is exact whenever no counter can
    /// overlap what the statement reads, and the statement is then the single shared cycle). Tally counts ADD into their counters (GR11); a
    /// REPLACING/CONVERTING result stores back through the target's <see cref="Place"/>, re-signing a signed
    /// numeric target with its retained original sign (GR4d).
    /// <para>An object-property identifier-1 or tally counter (kb/Work PB2078) needs no placement here: GR6 identifies every
    /// identifier once, as the first operation, and INSPECT has no phrase body, so the statement-level accessors
    /// (<c>OoBinder.OoWrapPropertyOps</c>: identifier-3 evaluated once, the GET before the statement, the SET after it) are
    /// exactly that identification.</para>
    /// </summary>
    public void Emit(BoundInspect statement)
    {
        var w = ctx.Writer;
        // ⛔ GR6 — ITEM IDENTIFICATION IS THE FIRST OPERATION, AND IT IS DONE ONCE (kb/Work PB1123): every operand's
        // subscripts / reference modifier are evaluated HERE, left to right, before identifier-1 is read or any counter
        // is stored — so a subscript that names a TALLYING counter (`YE(J) … TALLYING J`) still addresses the item
        // the statement identified when the replaced image is stored, and a REPLACING operand `T(ND)` reads the
        // occurrence ND named BEFORE the tally incremented it (GR19: a format 3's second pass shares the first's).
        var ins = IdentifyOperands(statement);
        int id = ctx.Names.NextInspectTmp();
        string img = $"__ins{id}";
        // identifier-1 is read as its character image whatever its SHAPE — a field, or (Format 1 only) a
        // function-identifier (PB10). AsString already dispatches on the operand kind, so admitting the function
        // case cost nothing here; it is the STORE below that the shape constrains.
        string readImage = OperandText.AsString(ins.Target, num, deSign: true);
        w.Line($"string {img} = {readImage};");
        // ⛔ GR2 — a ZERO-LENGTH identifier-1 (a dynamic-length item at length 0, an occurs-depending group at count
        // 0, a function returning a zero-length value — §8.5.4) leaves identifier-1 AND identifier-2 unchanged and
        // "control is immediately transferred to the end of the INSPECT statement" (kb/Work PB1128). Without the
        // test the tallying pass still ran `counter := counter + 0`, which is not a no-op: the arithmetic store
        // re-encodes identifier-2, so a counter holding a non-canonical image ("7 " in a PIC 99) came back "07".
        // The one image read above is the whole of item identification this statement then owes (GR6).
        using (w.Block($"if ({img}.Length != 0)"))
            EmitBody(ins, id, img, readImage);
    }

    /// <summary>§14.9.22.4 GR6 / §14.6.4 7): identify every identifier of the statement once, in SOURCE order —
    /// identifier-1, then each TALLYING counter with its pattern and delimiters, each REPLACING pattern / replacement /
    /// delimiters, then CONVERTING's operands — by freezing each <see cref="Place"/>'s run-time address into locals
    /// (<see cref="PlaceIdentification"/>). The returned statement addresses the locals; nothing after this reads a
    /// subscript variable again.</summary>
    private BoundInspect IdentifyOperands(BoundInspect ins)
    {
        var Hoist = PlaceIdentification.Hoister(ctx);
        BoundOperand? Identify(BoundOperand? op) => PlaceIdentification.Freeze(op, Hoist);

        var target = Identify(ins.Target)!;
        var tallying = new List<BoundInspectTally>(ins.Tallying.Count);
        foreach (var t in ins.Tallying)
        {
            var counter = PlaceIdentification.Freeze(t.Counter, Hoist);
            var pattern = Identify(t.Pattern);
            var before = Identify(t.Before);
            var after = Identify(t.After);
            tallying.Add(t with { Counter = counter, Pattern = pattern, Before = before, After = after });
        }
        var replacing = new List<BoundInspectReplace>(ins.Replacing.Count);
        foreach (var r in ins.Replacing)
        {
            var pattern = Identify(r.Pattern);
            var replacement = Identify(r.Replacement)!;
            var before = Identify(r.Before);
            var after = Identify(r.After);
            replacing.Add(r with { Pattern = pattern, Replacement = replacement, Before = before, After = after });
        }
        BoundInspectConvert? converting = null;
        if (ins.Converting is { } cv)
        {
            var from = Identify(cv.From)!;
            var to = Identify(cv.To)!;
            var before = Identify(cv.Before);
            var after = Identify(cv.After);
            converting = cv with { From = from, To = to, Before = before, After = after };
        }
        return ins with { Target = target, Tallying = tallying, Replacing = replacing, Converting = converting };
    }

    /// <summary>The TALLYING / REPLACING / CONVERTING passes and the write-back, over the non-empty image.</summary>
    private void EmitBody(BoundInspect ins, int id, string img, string readImage)
    {
        var w = ctx.Writer;
        string back = ins.Backward ? "true" : "false";
        var cat = CharacterCategoryOf(ins.Target);

        if (ins.Tallying.Count > 0)
        {
            var t = ins.Tallying;
            string kinds = string.Join(", ", t.Select(x => RuntimeApi.InspectTallyKindText(x.Kind)));
            string pats = string.Join(", ", t.Select(x => OperandTextOf(x.Pattern, cat)));
            string befs = string.Join(", ", t.Select(x => OperandTextOf(x.Before, cat)));
            string afts = string.Join(", ", t.Select(x => OperandTextOf(x.After, cat)));
            string tally = RuntimeApi.InspectTally(img, kinds, pats, befs, afts, back);
            w.Line($"long[] __cnt{id} = {tally};");
            // One add per operand, in source order — the same counter may appear under several operands and
            // accumulates each count (GR11 — INSPECT adds, it never initializes).
            // ⛔ A COUNTER THAT SHARES STORAGE WITH WHAT THE NEXT OPERAND READS (docs/CONFORMANCE.md D-INS1; the
            // standard leaves it undefined, §14.9.22.4 GR13 / Annex A.2 item 21 d)) CHANGES THAT READ: each operand's
            // count is taken from the statement's state AFTER the preceding operands stored theirs, so the image
            // and every operand value are read again and the cycle re-run. When no counter can overlap anything the
            // statement reads (the proof is conservative, StorageOverlap) the state never changes, the re-run would
            // return the same counts, and the ONE shared cycle above is the whole statement.
            for (int k = 0; k < t.Count; k++)
            {
                if (k > 0 && CounterMayChangeInputs(ins, t[k - 1].Counter))
                {
                    w.Line($"{img} = {readImage};");
                    w.Line($"__cnt{id} = {tally};");
                }
                arith.StoreArith(t[k].Counter,
                    num.Combine(num.FieldNum(t[k].Counter), "+", new NumX($"__cnt{id}[{k}]", 0), ReceiverContext.None),
                    CobolRounding.Truncation);
            }
            // ⛔ GR19: a format 3 is TWO SUCCESSIVE STATEMENTS, so the REPLACING half takes identifier-1 as the
            // tallying half left it (D-INS2). The one snapshot is exact only while no counter overlaps identifier-1.
            if (ins.Replacing.Count > 0 && ins.Tallying.Any(x => CounterMayOverlapTarget(ins, x.Counter)))
                w.Line($"{img} = {readImage};");
        }

        // REPLACING / CONVERTING over an operand that shares identifier-1's storage (§14.9.22.4 GR18 / GR21, Annex A.2
        // item 21 e) / f), undefined): docs/CONFORMANCE.md D-INS3 / D-INS4 — every operand value is read as the call's
        // arguments (the start of THIS pass), the cycle matches the pass's image, and the one write-back is the only
        // store, so an operand never observes the replacement.
        bool mutated = false;
        if (ins.Replacing.Count > 0)
        {
            var r = ins.Replacing;
            string kinds = string.Join(", ", r.Select(x => RuntimeApi.InspectReplaceKindText(x.Kind)));
            string pats = string.Join(", ", r.Select(x => OperandTextOf(x.Pattern, cat)));
            string reps = string.Join(", ", r.Select(x => OperandTextOf(x.Replacement, cat)));
            string befs = string.Join(", ", r.Select(x => OperandTextOf(x.Before, cat)));
            string afts = string.Join(", ", r.Select(x => OperandTextOf(x.After, cat)));
            // GR14: a figurative literal-3 takes the size of ITS pattern, which the runtime knows and the binder may
            // not (kb/Work PB1126) — so the flags travel, and the runtime fills.
            string? figs = r.Any(x => x.ReplacementIsFigurative)
                ? string.Join(", ", r.Select(x => x.ReplacementIsFigurative ? "true" : "false"))
                : null;
            w.Line($"{img} = {RuntimeApi.InspectReplace(img, kinds, pats, reps, befs, afts, back, figs)};");
            mutated = true;
        }

        if (ins.Converting is { } cv)
        {
            w.Line($"{img} = {RuntimeApi.InspectConvert(img, OperandTextOf(cv.From, cat), OperandTextOf(cv.To, cat), OperandTextOf(cv.Before, cat), OperandTextOf(cv.After, cat), back, cv.ToIsFigurative)};");
            mutated = true;
        }

        // ⛔ THE INVARIANT, ASSERTED RATHER THAN ASSUMED (PB10). `mutated` is true exactly for Formats 2/3/4 —
        // the formats in which §14.9.22.4 GR7/GR20 make identifier-1 a RECEIVING operand — and §8.4.3.2.3 SR1
        // bars a function-identifier from precisely those. So a non-field target reaching here means the
        // binder's COBOLNET1632 screen was bypassed, not that this statement needs a fallback: fail loud rather
        // than silently dropping the write-back, which would compute the replacement and discard it.
        if (mutated)
        {
            if (ins.Target is not BoundFieldOperand fieldTarget)
            {
                ctx.Writer.Line(EmitText.LoudStmt(
                    "INSPECT REPLACING/CONVERTING over a non-field identifier-1 (ISO §8.4.3.2.3 SR1 — the "
                    + "InspectBinder screen should have rejected this with COBOLNET1632)"));
                return;
            }
            EmitStore(fieldTarget.Place, img);
        }
    }

    /// <summary>Can storing <paramref name="counter"/> change what identifier-1 reads (§14.9.22.4 GR13's identifier-1
    /// overlap, GR19's format 3 hand-over)? A function-identifier identifier-1 is derived from its arguments at each
    /// read, which this cannot see through, so it answers yes.</summary>
    private static bool CounterMayOverlapTarget(BoundInspect ins, Place counter) => MayReadStorageOf(ins.Target, counter);

    /// <summary>Can storing <paramref name="counter"/> change anything the TALLYING cycle reads — identifier-1 or any
    /// operand's pattern or BEFORE/AFTER delimiter (§14.9.22.4 GR13: identifier-1, identifier-3 or identifier-4 in the
    /// storage of identifier-2)?</summary>
    private static bool CounterMayChangeInputs(BoundInspect ins, Place counter) =>
        CounterMayOverlapTarget(ins, counter)
        || ins.Tallying.Any(x => MayReadStorageOf(x.Pattern, counter) || MayReadStorageOf(x.Before, counter)
                                 || MayReadStorageOf(x.After, counter));

    /// <summary>Does reading <paramref name="op"/> possibly read the storage <paramref name="counter"/> names? A
    /// literal or a figurative never does; a field does when <see cref="StorageOverlap"/> cannot prove it disjoint;
    /// any other operand shape (a function-identifier's arguments) is not analysed and answers yes.</summary>
    private static bool MayReadStorageOf(BoundOperand? op, Place counter) => op switch
    {
        null or BoundStringLiteral or BoundNumericLiteral or BoundFigurative or BoundAllLiteral => false,
        BoundFieldOperand f => StorageOverlap.MayShareStorage(counter, f.Place),
        _ => true,
    };

    /// <summary>Store the replaced/converted image back into identifier-1 by its storage shape: a group through the
    /// ONE group value writer in the alphabet it was read in (a Tier-B view group splices its window); an elementary
    /// item (alphanumeric / numeric-edited / a numeric item's character image) takes the image directly — except a
    /// SIGNED numeric item, whose replaced digits are re-signed with the RETAINED original sign, a zero magnitude
    /// included (§14.9.22.4 GR4 d, <see cref="CobolNum.RetainSign"/>). A replacement that left a non-digit in a signed
    /// item decodes by digit positions only — deterministic where the spec leaves the result undefined (§14.6.13.2
    /// incompatible data).</summary>
    private void EmitStore(Place p, string img)
    {
        var w = ctx.Writer;
        // A reference-modified identifier-1 is the elementary alphanumeric unique item of §8.4.3.3.4 GR6 — the
        // replaced image splices into it (kb/Work PB70: over a GROUP inner it used to fall into the group arm).
        if (p is RefModPlace) { w.Line(PlaceRenderer.Write(p, img)); return; }
        // A group identifier-1 (§14.9.22.3 SR1 — "an alphanumeric or national group item"): the ONE group VALUE
        // writer, in the alphabet the read used (a national group's national positions, never its byte image —
        // kb/Work PB1128), over the SENDING extent — §14.9.22.4 GR1 sizes identifier-1 "as a sending data item", so
        // an occurs-depending group splices back over exactly its current-count part, positions past it unmodified.
        if (p.Item.IsGroup)
        {
            w.Line(PlaceRenderer.WriteGroupValue(p, img, "INSPECT REPLACING/CONVERTING into group", AccessDir.Sending));
            return;
        }
        // ⛔ A NUMERIC ELEMENTARY identifier-1 IS CHARACTER-IMAGE STORED — INSPECT is its character channel
        // (UsageCollectionPass.Visit(BoundInspect), kb/Work PB1128). REPLACING / CONVERTING deposit characters, and
        // GR4 d) says "if identifier-1 is a signed numeric item, the original value of the sign is retained upon
        // completion of the INSPECT statement" — a native value carrier holds no negative zero, so the replaced
        // image went through a `long` and `S9(3) VALUE -5` `REPLACING ALL "5" BY "0"` came back +0 (`00{` for `00}`).
        // A Tier-B view and a promoted leaf are the whole shape set (the invariant below). An OCCURS DYNAMIC element
        // is a promoted leaf too: its occurrence is promoted by the same rule (StorageFormPass.Classify), so the
        // table is a CobolDynTable<string> and the element holds the negative zero (kb/Work PB2004).
        if (p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } pic)
        {
            if (!(p.Item.StoreAsImage || p is RedefViewPlace))
                throw new InvalidOperationException(
                    $"INSPECT identifier-1 '{p.Item.CsName}' is a numeric item stored natively; UsageCollectionPass must have promoted it to its character image (PB1128)");
            // ⛔ THE REPLACED IMAGE OF A NUMERIC ITEM IS NOT AN ALPHANUMERIC SENDING OPERAND. The signed arm takes
            // CobolNum.DigitMagnitude, never the §14.9.25.4 GR6 d) 3 capped FromAlphanumeric: this image is the ITEM'S
            // OWN and its PICTURE already fixes the size, so GR6 d) 3 asks nothing here (kb/Work PB426 split the two
            // decodes for that reason). A replacement that left a non-digit in a SIGNED item decodes by digit
            // positions only — deterministic where the spec leaves the result undefined (§14.6.13.2 incompatible data);
            // an unsigned item holds the replaced characters as they are.
            if (pic.Signed)
            {
                // Re-sign the replaced digit run with the sign the ORIGINAL image carries, in the item's sign
                // convention (GR4 d). The sign is read off the image and handed to the formatter APART from the
                // magnitude: a value cannot carry one over a zero, so formatting `parse(image) < 0 ? -mag : mag` turned
                // a negative item whose digits became zeros into +0. The sign read is the STORE side re-deriving the
                // ORIGINAL's sign, not INSPECT's sending read of the operand (§14.6.13.2 rule 2 attaches to the
                // reference of the content, which is the image read above; a second check inside one statement
                // would report one reference as two).
                w.Line(PlaceRenderer.Write(p, RuntimeApi.NumRetainSign(img, PlaceRenderer.Read(p), p.Item.ProfileName)));
                return;
            }
        }
        w.Line(PlaceRenderer.Write(p, img));   // character-image stored: alphanumeric, numeric-edited, a numeric item's image
    }

    /// <summary>An INSPECT operand as the runtime's C# string argument, or <c>null</c> when absent (a CHARACTERS
    /// pattern / an omitted delimiter). An identifier operand reads its FULL raw image at run time — current
    /// content, no trimming (GR5; a PIC X(2) holding "A " is the two-character pattern "A "); a signed numeric
    /// operand reads de-signed (GR4d).
    /// <para>⛔ <paramref name="cat"/> IS THE INSPECTED ITEM'S CATEGORY (kb/Work PB1414): an INSPECT operand's
    /// context is identifier-1's characters, so a figurative HIGH-/LOW-VALUE operand is read from the NATIONAL
    /// program collating sequence when identifier-1 is national and from the alphanumeric one otherwise
    /// (§8.3.3.6.4 GR6/GR7 — the same rule <c>OperandText.AsString</c>'s characterCategory answers for STRING and
    /// UNSTRING, kb/Work PB1185).</para></summary>
    private string OperandTextOf(BoundOperand? op, PicCategory? cat) =>
        op is null ? "null" : OperandText.AsString(op, num, deSign: true, characterCategory: cat);

    /// <summary>The character CONTEXT a figurative operand is read in: identifier-1's §8.5.2.1 Table 2 CLASS, never
    /// its raw PICTURE category (kb/Work PB1414). The category test this replaced read <c>Pic.Category</c>, which
    /// is null for EVERY GROUP — so a GROUP-USAGE NATIONAL identifier-1 took the alphanumeric collating sequence —
    /// and for a function-identifier. The class answers all of them: national ⇒ the national program collating
    /// sequence, boolean ⇒ the pinned boolean fill, everything else ⇒ the alphanumeric one.</summary>
    private static PicCategory CharacterCategoryOf(BoundOperand target) =>
        IntrinsicArgumentRules.ClassOf(target) switch
        {
            ClassKind.National => PicCategory.National,
            ClassKind.Boolean => PicCategory.Boolean,
            _ => PicCategory.Alphanumeric,
        };
}
