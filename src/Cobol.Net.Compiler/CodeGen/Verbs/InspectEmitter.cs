// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

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
    /// writes identifier-1, so the one snapshot is exact). Tally counts ADD into their counters (GR11); a
    /// REPLACING/CONVERTING result stores back through the target's <see cref="Place"/>, re-signing a signed
    /// numeric target with its retained original sign (GR4d).
    /// </summary>
    public void Emit(BoundInspect ins)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextInspectTmp();
        string img = $"__ins{id}";
        // identifier-1 is read as its character image whatever its SHAPE — a field, or (Format 1 only) a
        // function-identifier (PB10). AsString already dispatches on the operand kind, so admitting the function
        // case cost nothing here; it is the STORE below that the shape constrains.
        w.Line($"string {img} = {OperandText.AsString(ins.Target, num, deSign: true)};");
        // ⛔ GR2 — a ZERO-LENGTH identifier-1 (a dynamic-length item at length 0, an occurs-depending group at count
        // 0, a function returning a zero-length value — §8.5.4) leaves identifier-1 AND identifier-2 unchanged and
        // "control is immediately transferred to the end of the INSPECT statement" (kb/Work PB1128). Without the
        // test the tallying pass still ran `counter := counter + 0`, which is not a no-op: the arithmetic store
        // re-encodes identifier-2, so a counter holding a non-canonical image ("7 " in a PIC 99) came back "07".
        // The one image read above is the whole of item identification this statement then owes (GR6).
        using (w.Block($"if ({img}.Length != 0)"))
            EmitBody(ins, id, img);
    }

    /// <summary>The TALLYING / REPLACING / CONVERTING passes and the write-back, over the non-empty image.</summary>
    private void EmitBody(BoundInspect ins, int id, string img)
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
            w.Line($"long[] __cnt{id} = {RuntimeApi.InspectTally(img, kinds, pats, befs, afts, back)};");
            // One add per operand, in source order — the same counter may appear under several operands and
            // accumulates each count (GR11 — INSPECT adds, it never initializes).
            for (int k = 0; k < t.Count; k++)
                arith.StoreArith(t[k].Counter,
                    num.Combine(num.FieldNum(t[k].Counter), "+", new NumX($"__cnt{id}[{k}]", 0), ReceiverContext.None),
                    CobolRounding.Truncation);
        }

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

    /// <summary>Store the replaced/converted image back into identifier-1 by its storage shape: a group through the
    /// ONE group value writer in the alphabet it was read in (a Tier-B view group splices its window); a string-stored elementary
    /// item (alphanumeric / numeric-edited / zoned image) takes the image directly; a native-stored numeric DISPLAY
    /// item re-encodes the digit image — re-applying the RETAINED original sign for a signed item (§14.9.22.4
    /// GR4d). A replacement that left a non-digit in a numeric item decodes by digit positions only — deterministic
    /// where the spec leaves the result undefined (§14.6.13.2 incompatible data).</summary>
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
        // ⛔ THE REPLACED IMAGE OF A NUMERIC ITEM IS NOT AN ALPHANUMERIC SENDING OPERAND. Every decode below
        // takes CobolNum.DigitMagnitude, never the §14.9.25.4 GR6 d) 3 capped FromAlphanumeric: this image
        // is the ITEM'S OWN and its PICTURE already fixes the size, so GR6 d) 3 asks nothing here. kb/Work PB426
        // split the two decodes for that reason — MEASURED: pointing this site at the capped entry changed no
        // answer on any shape probed (including a group-aliased PIC S9(31) SIGN TRAILING SEPARATE, whose image
        // IS 32 characters), so this is rule hygiene, not a second bug fix.
        if (p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } pic)
        {
            bool stringStored = p.Item.StoreAsImage || p is RedefViewPlace || p is RefModPlace;
            if (!stringStored)
            {
                if (pic.Signed)
                {
                    // GR4d: the original sign is retained — the (still-unmodified) field supplies it.
                    string mag = $"__insMag{ctx.Names.NextInspectTmp()}";
                    w.Line($"var {mag} = {ArithmeticEmitter.Narrow(RuntimeApi.NumDigitMagnitude(img), p.Item)};");
                    w.Line(PlaceRenderer.Write(p, $"({PlaceRenderer.Read(p)} < 0 ? -{mag} : {mag})"));
                }
                else
                    w.Line(PlaceRenderer.Write(p, ArithmeticEmitter.Narrow(RuntimeApi.NumDigitMagnitude(img), p.Item)));
                return;
            }
            if (pic.Signed)
            {
                // A string-stored signed zoned image (whole-group-aliased / Tier-B view): decode the original for
                // its sign, re-encode the replaced magnitude with that sign in the item's sign convention (GR4d).
                string mag = $"__insMag{ctx.Names.NextInspectTmp()}";
                w.Line($"Int128 {mag} = {RuntimeApi.NumDigitMagnitude(img)};");
                // sending: false — this decode is the STORE side re-deriving the ORIGINAL's sign so the replaced
                // magnitude keeps it (GR4d), not INSPECT's sending read of the operand. §14.6.13.2 rule 2 attaches to
                // the reference of the content, which is the image read above; checking it twice inside one statement
                // would report one reference as two.
                w.Line(PlaceRenderer.Write(p, RuntimeApi.NumFormatImage(
                    $"{RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending: false)} < 0 ? -{mag} : {mag}", p.Item.ProfileName)));
                return;
            }
        }
        w.Line(PlaceRenderer.Write(p, img));   // string-stored: alphanumeric, numeric-edited, unsigned zoned image, ref-mod slice
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

    private static PicCategory? CharacterCategoryOf(BoundOperand target) =>
        target is BoundFieldOperand { Place.Item.Pic: { } pic } ? pic.Category : null;
}
