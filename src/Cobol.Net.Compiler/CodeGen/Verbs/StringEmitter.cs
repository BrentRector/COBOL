// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The STRING / UNSTRING verb emitter (P7 Step 9d — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter.StringUnstring partial). Every runtime-member
/// fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class StringEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith, EcEmitter ec, MoveEmitter move)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the ON/NOT-ON
    /// OVERFLOW phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>STRING (ISO §14.9.43): the receiver's character image is materialized into ONE working local (its
    /// CURRENT content — GR7 preserves every position the transfer does not touch; there is no space filling), each
    /// sending operand transfers into it via <c>StringTransfer</c> in statement order (GR3), and the
    /// final image stores back through the receiver's own store path. The pointer initializes from the POINTER item
    /// (GR4) or 1 (GR5), advances only with the per-character moves (GR6 — the runtime kernel), and writes back —
    /// before the overflow phrases run, which may inspect it — only when the phrase was written. The overflow flag
    /// is latched across sendings by the kernel (GR8a) and dispatches ON / NOT ON OVERFLOW per GR8c/GR8e/GR9 (the
    /// 2002+ EC-OVERFLOW-STRING name, GR8b, awaits the EC model; with no phrase the nonfatal condition continues
    /// execution, §14.6.13.1.4, so no code is needed).</summary>
    public void EmitString(BoundStringStmt s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextStrUnstr();
        string ptr = $"__strPtr{id}", ptr0 = $"__strPtr0{id}", ovf = $"__strOvf{id}", acc = $"__strInto{id}";
        w.Line(s.Pointer is { } p0
            ? $"long {ptr} = {RuntimeApi.HostInt64(NumericRenderer.Align(num.AsNum(new BoundFieldOperand(p0), ReceiverContext.None), 0))};"   // GR4 — the user's initial value (by VALUE — kb/Work PB86; saturating — PB1033)
            : $"long {ptr} = 1;");                                                    // GR5 — implicit pointer of 1
        w.Line($"long {ptr0} = {ptr};");   // the starting pointer: GR6 changes the POINTER item only when a character moves
        w.Line($"bool {ovf} = false;");
        // ⚖ A DYNAMIC-LENGTH identifier-3 (kb/Work PB871; DETERMINATION D-DL2, docs/CONFORMANCE.md §3): GR6 and
        // GR8 are written over "the number of character positions in the data item referenced by identifier-3",
        // which for a dynamic-length receiver is its MAXIMUM size (ReceivingStore.DynamicReceivingSize) — the
        // current length made an empty item unwritable (every character an overflow). The working image is the
        // current content widened to that size with spaces — the characters §14.9.39.4 GR39 gives any position a
        // dynamic-length item grows by — so GR7's "all other portions ... will contain data that was present
        // before" holds for every position the content already had. The new length is §8.5.1.10.4's "length of
        // new content": the old content's, extended through the last position this execution wrote.
        // Place.DenotedItem is the ONE "whole item" question (§8.4.3.3.4 GR5; kb/Work PB602): a reference-modified
        // identifier-3 denotes no item, and §8.5.1.10.4 makes it a fixed-length item of the current length.
        bool dynInto = s.Into.DenotedItem is { IsDynamicLength: true };
        string old = $"__strOld{id}";
        if (dynInto)
        {
            w.Line($"string {old} = {ReadImage(s.Into)};");
            w.Line($"string {acc} = {old}.PadRight({ReceivingStore.DynamicReceivingSize(s.Into.Item)});");
        }
        else
            w.Line($"string {acc} = {ReadImage(s.Into)};");
        foreach (var snd in s.Sendings)
        {
            // GR3a: the sender's CONTENT transfers per the alphanumeric-to-alphanumeric move mechanics — its raw
            // character image (a numeric sender contributes its sign-carrying zoned image), not a converted value.
            // GR2: a figurative literal-1 / literal-2 is a one-character item of identifier-3's usage, so its
            // HIGH-/LOW-VALUE comes from THAT usage's program collating sequence (§8.3.3.6.4 GR6; kb/Work PB1185).
            string src = OperandText.AsString(snd.Value, num, characterCategory: s.CharacterCategory);
            string delim = snd.BySize || snd.Delimiter is null ? "null"
                : OperandText.AsString(snd.Delimiter, num, characterCategory: s.CharacterCategory);
            w.Line($"{acc} = {RuntimeApi.StrTransfer(acc, src, delim, ptr, ovf)};");
        }
        if (dynInto)
            // GR6 advances the pointer once per character moved, so a moved character's position is below the
            // final pointer: the content reaches position ptr-1 exactly when anything moved (ptr != its start).
            w.Line(PlaceRenderer.Write(s.Into, ReceivingStore.Characters(s.Into.Item,
                $"{acc}.Substring(0, {ptr} != {ptr0} ? System.Math.Max({old}.Length, (int)({ptr} - 1)) : {old}.Length)", "")));
        else
            WriteImage(s.Into, acc);
        // GR6: the pointer item changes only as characters move. Storing it back unconditionally re-wrote an
        // unchanged pointer through its host carrier — a saturated value past long (kb/Work PB1033) — so it is
        // stored only when it moved, and a pointer that overflowed before any transfer keeps its exact value.
        if (s.Pointer is { } p)
            using (w.Block($"if ({ptr} != {ptr0})"))
                arith.StoreArith(p, new NumX(ptr, 0), CobolRounding.Truncation);
        EmitOverflow(ovf, "EC-OVERFLOW-STRING", s.OnOverflow, s.NotOnOverflow);   // GR8b
    }

    /// <summary>UNSTRING (ISO §14.9.48): the sender's image and the delimiter values are read ONCE at initiation
    /// (operand overlap is undefined, GR18), the pointer initializes from the POINTER item or 1 (GR11a) and the
    /// tally from the TALLYING item's CURRENT value (GR14 — the statement ADDS to it). An initiation pointer
    /// outside [1, size(sender)] is the GR15a overflow and TERMINATES the operation before any transfer (GR16a — a
    /// check the legacy engine performed but did not honor with termination); otherwise each receiving area gets
    /// one <c>UnstringExtract</c> (GR11b–f), its result stored per the MOVE rules (GR11c — so two
    /// contiguous delimiters space-fill an alphanumeric receiver and ZERO-fill a numeric one, GR8), DELIMITER IN /
    /// COUNT IN stored per GR11d/e, and the tally bumped — all skipped when the sender was already exhausted
    /// (GR11g: that receiver is not acted upon; exhaustion is NOT overflow, GR15). After the receivers, unexamined
    /// sender characters with every receiver acted upon raise the GR15b overflow. Pointer/tally write back before
    /// the ON / NOT ON OVERFLOW dispatch (GR16c/GR16e/GR17; EC-OVERFLOW-UNSTRING, GR16b, awaits the EC model).</summary>
    public void EmitUnstring(BoundUnstringStmt s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextStrUnstr();
        string src = $"__unsSrc{id}", dels = $"__unsDel{id}", alls = $"__unsAll{id}",
               ptr = $"__unsPtr{id}", tly = $"__unsTly{id}", ovf = $"__unsOvf{id}";
        w.Line($"string {src} = {OperandText.AsString(s.Source, num)};");   // DA4: an operand (may be a function)
        if (s.Delimiters.Count > 0)
        {
            // GR10: applied in statement order (the kernel's earliest-match-then-first-listed scan); a figurative
            // is its single character (GR7) — a NATIONAL literal when identifier-1 is national, so its HIGH-/LOW-VALUE
            // is the national sequence's (§8.3.3.6.4 GR6; kb/Work PB1185); a field delimiter is its FULL content —
            // trailing spaces included (GR9: the delimiter is the content of the item; the legacy's TrimEnd was a
            // deviation).
            w.Line($"string[] {dels} = {{ {string.Join(", ", s.Delimiters.Select(d => OperandText.AsString(d.Value, num, characterCategory: s.CharacterCategory)))} }};");
            w.Line($"bool[] {alls} = {{ {string.Join(", ", s.Delimiters.Select(d => d.All ? "true" : "false"))} }};");
        }
        else
        {
            w.Line($"string[] {dels} = System.Array.Empty<string>();");
            w.Line($"bool[] {alls} = System.Array.Empty<bool>();");
        }
        w.Line(s.Pointer is { } p0
            ? $"long {ptr} = {RuntimeApi.HostInt64(NumericRenderer.Align(num.AsNum(new BoundFieldOperand(p0), ReceiverContext.None), 0))};"   // GR11a / GR12 — user-initialized (by VALUE — kb/Work PB86; saturating — PB1033)
            : $"long {ptr} = 1;");                                                    // GR11a — leftmost position
        w.Line($"long {ptr}__0 = {ptr};");
        w.Line(s.Tallying is { } t0
            ? $"Int128 {tly} = {NumericRenderer.Align(num.AsNum(new BoundFieldOperand(t0), ReceiverContext.None), 0)};"   // GR14 — adds to the current value (by VALUE — kb/Work PB86), EXACT: a count is summed, never narrowed (PB1033)
            : $"Int128 {tly} = 0;");
        w.Line($"bool {ovf} = false;");
        using (w.Block($"if ({ptr} < 1 || {ptr} > {src}.Length)"))
            w.Line($"{ovf} = true;");                                                 // GR15a; GR16a terminates — no transfer
        using (w.Block("else"))
        {
            // GR11 b)'s examination size is read only when it can govern: with no DELIMITED phrase, or when every
            // delimiter is an IDENTIFIER — GR9: "When neither literal-1 nor literal-2 is specified and all data items
            // referenced by identifier-2 and identifier-3 are zero-length items, it is as if the DELIMITED phrase
            // were not specified". A literal delimiter is never zero-length (SR1), so one of them settles it; an
            // identifier is a data item OR a function-identifier (§8.4.3.1.2), and either may be zero-length.
            bool sizeCanGovern = s.Delimiters.All(d => d.Value is BoundFieldOperand or BoundComputedOperand);
            for (int k = 0; k < s.Receivers.Count; k++)
            {
                var r = s.Receivers[k];
                string cnt = $"__unsCnt{id}_{k}", fld = $"__unsFld{id}_{k}", dlm = $"__unsDlm{id}_{k}";
                // GR11 b): "the size of the current receiving area" — a property of the receiver AT EXECUTION (an
                // ANY LENGTH formal's argument length, a reference modifier's evaluated length), asked of the ONE
                // receiving-size reader (kb/Work PB979: this was a binder integer, 1 for ANY LENGTH, and a
                // reference-modified receiver was staged as not implemented).
                string size = sizeCanGovern ? ReceivingStore.ExaminationSize(r.Target) : "0";
                w.Line($"long {cnt} = {RuntimeApi.UnstringExtract(src, dels, alls, size, ptr, fld, dlm)};");
                using (w.Block($"if ({cnt} >= 0)"))                                   // −1: not acted upon (GR11g)
                {
                    // GR11 c): the examined characters ARE the conceptual elementary item, moved "according to
                    // the rules for the MOVE statement" — by the bound MOVE, never a private copy of its rules.
                    w.Line(PlaceRenderer.Write(s.Examined, ReceivingStore.Characters(s.Examined.Item, fld, "")));
                    if (r.ZeroFill is { } zero)
                    {
                        // GR8: two contiguous delimiters zero-fill a NUMERIC receiver (the MOVE rules would give
                        // the zero-length sender's SPACE, §14.9.25.4 GR1/GR2).
                        using (w.Block($"if ({cnt} == 0)")) move.Emit(zero);
                        using (w.Block("else")) move.Emit(r.Store);
                    }
                    else
                        move.Emit(r.Store);
                    if (r.DelimiterStore is { } ds)
                    {
                        // GR11 d): the delimiting characters, the same conceptual-item shape; an end-of-data
                        // delimiting condition leaves them empty, which the MOVE rules space-fill (GR1/GR2).
                        w.Line(PlaceRenderer.Write(s.Delimiting!, ReceivingStore.Characters(s.Delimiting!.Item, dlm, "")));
                        move.Emit(ds);
                    }
                    if (r.CountIn is { } ci) arith.StoreArith(ci, new NumX(cnt, 0), CobolRounding.Truncation);   // GR11e
                    w.Line($"{tly} += 1;");                                           // GR14 — per receiver acted upon
                }
            }
            w.Line($"if ({ptr} <= {src}.Length) {ovf} = true;   // unexamined characters remain (ISO §14.9.48.4 GR15b)");
        }
        // GR13 — stored only when the pointer moved (the STRING twin's reason: kb/Work PB1033), so a pointer that
        // was out of range before any examination keeps its exact value.
        if (s.Pointer is { } p)
            using (w.Block($"if ({ptr} != {ptr}__0)"))
                arith.StoreArith(p, new NumX(ptr, 0), CobolRounding.Truncation);
        if (s.Tallying is { } t) arith.StoreArith(t, new NumX(tly, 0), CobolRounding.Truncation);    // GR14
        EmitOverflow(ovf, "EC-OVERFLOW-UNSTRING", s.OnOverflow, s.NotOnOverflow);   // GR16b
    }

    /// <summary>The shared ON / NOT ON OVERFLOW dispatch (STRING GR8c/8e/GR9; UNSTRING GR16c/16e/GR17): the ON
    /// imperative runs exactly when the flag is set, the NOT imperative exactly when it is not; with neither
    /// phrase the (nonfatal) condition lets execution continue, §14.6.13.1.4. Under enabled EC-OVERFLOW checking
    /// (>>TURN, §7.3.25) the raise (status + the no-phrase F3 selection) precedes the phrase branch — STRING
    /// GR8b / UNSTRING GR16b via the <see cref="EcEmitter"/> overflow emission.</summary>
    private void EmitOverflow(
        string flag, string ecName, IReadOnlyList<BoundStatement>? onOverflow, IReadOnlyList<BoundStatement>? notOnOverflow)
    {
        var w = ctx.Writer;
        ec.EmitOverflow(flag, ecName, hasPhrase: onOverflow is not null);
        if (onOverflow is { } on)
        {
            using (w.Block($"if ({flag})")) Statements.EmitStatementList(on);
            if (notOnOverflow is { } notAlso)
                using (w.Block("else")) Statements.EmitStatementList(notAlso);
        }
        else if (notOnOverflow is { } notOnly)
            using (w.Block($"if (!{flag})")) Statements.EmitStatementList(notOnly);
    }

    /// <summary>The character image of a STRING/UNSTRING character-position operand — its raw content (a group's
    /// concatenated image, a numeric-DISPLAY item's sign-carrying zoned image): the verbs operate on character
    /// positions, never converted values (STRING GR3a / UNSTRING GR11).</summary>
    private string ReadImage(Place p) => OperandText.AsString(new BoundFieldOperand(p), num);

    /// <summary>Store a full-width character image back into the STRING receiver, preserving its storage shape
    /// (§14.9.43.4 GR7 — the image already carries the untouched positions): a character-image group distributes
    /// via <c>FromImage</c>; a Tier-B view / reference window splices through its own <c>Write</c>; a long-stored
    /// numeric-DISPLAY receiver (SR1 admits usage-display numeric) decodes the updated zoned image back to its
    /// value; an alphanumeric / image-stored receiver assigns the image directly (same width by construction).</summary>
    private void WriteImage(Place p, string imageExpr)
    {
        var w = ctx.Writer;
        // A reference-modified identifier-3 is the elementary alphanumeric unique item of §8.4.3.3.4 GR6 — the
        // updated image splices into it (kb/Work PB70: over a GROUP inner it used to fall into the group arm).
        if (p is RefModPlace) { w.Line(PlaceRenderer.Write(p, imageExpr)); return; }
        // A group identifier-3 (§14.9.43.4 GR3a — the alphanumeric MOVE rules): the ONE group-image store.
        // The ONE group VALUE writer, in the alphabet ReadImage read (SendingGroupValue): a national group — class
        // national, so legal here by SR1 — takes its national positions, never its byte image (kb/Work PB1128).
        if (p.Item.IsGroup) { w.Line(PlaceRenderer.WriteGroupValue(p, imageExpr, "STRING INTO group")); return; }
        if (p is not RedefViewPlace && !p.Item.StoreAsImage
            && p.Item.Pic is { IsCharacterFormNumeric: true })   // THE ONE character-form predicate (kb/Work PB646)
        {
            w.Line(PlaceRenderer.Write(p, ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(imageExpr, p.Item.ProfileName), p.Item)));
            return;
        }
        if (p is not RedefViewPlace && !p.Item.StoreAsImage
            && p.Item.Pic is not { Category: PicCategory.Alphanumeric })
        {
            w.Line(LoudStmt($"STRING INTO receiver '{p.Item.CobolName}' (usage display required, ISO §14.9.43.3 SR1)"));
            return;
        }
        w.Line(PlaceRenderer.Write(p, imageExpr));
    }
}
