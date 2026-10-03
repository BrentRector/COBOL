// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The MOVE-family emitter (P7 Step 9g — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the orchestrator partial): the pure per-kind MOVE renderer
/// (the dispatch travels on <c>BoundMove.Kinds</c>, P7 Step 7) and <c>ConvertSource</c> — the ONE MOVE
/// conversion path Report Writer's synthetic print items and INITIALIZE's implicit stores share.</summary>
internal sealed class MoveEmitter(EmitContext ctx, NumericRenderer num, ReferenceResolver refs)
{
    /// <summary>Render one MOVE — a PURE per-store renderer since P7 Step 7: the dispatch travels on the node
    /// (<see cref="BoundMove.Stores"/>, classified once by <see cref="MoveClassifier"/> at construction — ISO
    /// §14.9.25.4 GR4's elementary-vs-group decision incl. the sender side, NC105A MOVE-TEST-F1-16/-17/-20/-36/-38);
    /// the emitter re-derives nothing.
    /// <para>⛔ The SENDER is read per target off the store, never off <c>m.Source</c>: §14.9.25.4 GR2/GR3
    /// substitute the figurative constant SPACE / ZERO for a zero-length literal-1 PER RECEIVING OPERAND
    /// (excluding a dynamic-length one), so one statement can legitimately store two different senders
    /// (kb/Work PB425). Rendering <c>m.Source</c> here is the defect that shape prevents.</para></summary>
    public void Emit(BoundMove m)
    {
        for (int i = 0; i < m.Targets.Count; i++)
        {
            var target = m.Targets[i];
            var (source, kind, origin) = m.Stores[i];
            // ⛔ ISO §14.9.25.4 GR1's ZERO-LENGTH-ITEM clause — "If identifier-1 is a zero-length item, it is as
            // if literal-1 were specified as a zero-length literal" — which lands in the GR2/GR3 substitution the
            // store above already carries for a written literal. Whether the sending ITEM is zero-length is a
            // RUNTIME state (§8.5.1.10), so this is the one arm of the rule that must be a runtime test; the
            // classifier decides WHEN one is needed and over which sending place (kb/Work PB425). The length is
            // read ONCE and both arms are the two stores the rule names, side by side.
            if (MoveClassifier.ZeroLengthItemRoute(source, target) is { } zlSend)
            {
                // The LENGTH to test is the sending operand's CURRENT one. For the elementary shapes (§8.5.4
                // items 3, 4, and 6/9 after their materialization) that is the carrier's own `.Length`; for a
                // GROUP (items 1, 2, 5, 7 — kb/Work PB896) it is the group's SENDING VALUE, which §13.18.38.4
                // GR8 a) already narrows to the occurs-depending current extent — through THE ONE group value
                // reader, because a bit or national group's plain Read() is its STRUCT and has no length at all
                // (kb/Work PB943: `NG.Length`, CS1061). Asking the plain place would also read the MAXIMUM extent
                // and the test could never fire.
                string len = zlSend.CurrentRecord is { } curRec ? OperandText.CurrentRecordLength(curRec)   // §8.5.4 item 5 — the record length is §13.18.43.4 GR16's byte count
                    : zlSend.Place.Item.IsGroup
                    ? $"{PlaceRenderer.SendingGroupValue(zlSend.Place, "zero-length-item test of")}.Length"
                    : $"{PlaceRenderer.Read(zlSend.Place)}.Length";
                // ⛔ BOTH ARMS ARE THE STATEMENT'S OWN DISPATCH. GR1's substitution changes the move's KIND — a
                // group sender becomes a literal one, so GR4's first sentence makes the zero-length arm an
                // ELEMENTARY move while the non-zero arm stays the group move (kb/Work PB896) — and the zero arm's
                // kind is whatever the substituted figurative dispatches to against THIS receiver: the image fill
                // into a numeric one, the edited fill into an edited one, a slice fill into a reference-modified
                // one, a group fill into a group (kb/Work PB943 — the arm used to be hard-wired to the elementary
                // store, which is why the route could only ever serve the numeric receivers it was filtered to).
                var fig = MoveClassifier.ZeroLengthItemFigurative(zlSend);
                ctx.Writer.Line($"if ({len} == 0) {{");
                EmitStore(target, fig, MoveClassifier.Kind(fig, target), MoveSenderOrigin.ZeroLengthItem);
                ctx.Writer.Line("} else {");
                EmitStore(target, source, kind, origin, m.Stores[i].Prefill);
                ctx.Writer.Line("}");
                continue;
            }
            EmitStore(target, source, kind, origin, m.Stores[i].Prefill);
            EmitElementMoves(m, i);
        }
    }

    /// <summary>ISO §14.6.9.2's last sentence, after a §14.9.25.4 GR9 group transfer into receiver
    /// <paramref name="target"/>: "Correspondingly numbered elements are moved according to the rules of the MOVE
    /// statement" (kb/Work PB1144). The group transfer moved each corresponding table as element IMAGES and fixed
    /// the receiving table's capacity (recreated from the sender, raised to its minimum, §14.6.9.2); each pair whose
    /// elementary elements differ in description then has its elements re-moved by the BOUND element MOVE — the
    /// ONE MOVE path, so the conversion is §14.9.25.4 GR6's and never a second copy of it. The loop runs over
    /// §14.6.9.1's current capacities read after the transfer, to the smaller of the two: rule 1's superfluous
    /// sending elements are not moved, and rule 2's receiving elements past the sender stay space filled. Reached
    /// only from the GR9 arm (the zero-length-item route excludes it, so the two arms never interleave).</summary>
    private void EmitElementMoves(BoundMove m, int target)
    {
        var w = ctx.Writer;
        foreach (var e in m.ElementMoves)
        {
            if (e.Target != target) continue;
            if (e is not { Move: { } move, SendingCount: { } sc, ReceivingCount: { } rc })
            {
                w.Line(LoudStmt(e.Unsupported ?? "the element moves of a variable-length group MOVE (ISO §14.6.9.2)"));
                continue;
            }
            using (w.Block($"for (long {e.Var} = 1, {e.Var}n = System.Math.Min((long)({PlaceRenderer.OccurrenceCount(sc)}), "
                    + $"(long)({PlaceRenderer.OccurrenceCount(rc)})); {e.Var} <= {e.Var}n; {e.Var}++)"))
                Emit(move);
        }
    }

    /// <summary>The store of ONE receiving operand, by its bound <see cref="MoveKind"/> — factored out of
    /// <see cref="Emit"/> so §14.9.25.4 GR1's zero-length-item test can place the statement's REAL store in its
    /// else arm (kb/Work PB896). Every arm is the one it always was; the only change is that the dispatch has a
    /// name and two callers.</summary>
    private void EmitStore(Place target, BoundOperand source, MoveKind kind, MoveSenderOrigin origin,
        OverlapPrefill prefill = default)
    {
        EmitOverlapPrefill(target, prefill);
        switch (kind)
        {
            case MoveKind.RefModSlice:
                // The slice takes the source's characters (SpliceInto left-justifies, space-fills, and
                // truncates to the slice length), so pass the raw image, not a full-width store. EXCEPT a
                // figurative source — a figurative word AND `ALL literal-1`, both Format-6/7 figurative constants
                // — which fills EVERY position of the slice (ISO §8.3.3.6.4 GR2 — "the string of characters is
                // repeated character by character until the size of the resultant string is greater than or equal
                // to the number of character positions in the associated data item"; §8.4.3.3.4 GR5 makes the
                // slice that data item). The ALL literal used to fall to the plain splice and land ONCE,
                // space-padded (kb/Work PB1655). The figurative word's fill char is category-aware
                // (national/boolean = the D-N3 pin, not the PCS extreme).
                var rmp = (RefModPlace)target;
                // ⛔ OperandPic, NOT raw `Pic` — THE SECOND ARM of kb/Work PB173's pad defect (its twin is
                // PlaceRenderer.Write's RefModPlace boolean pad). `Pic` is null for any GROUP, so a
                // figurative fill into a BIT-GROUP slice chose the alphanumeric SPACE instead of the boolean
                // zero §14.6.8.6 requires. `OperandPic` answers the as-if PICTURE 1(m) of §13.18.29.4 GR1b —
                // the ONE category reader, the same one RefModPlace.Category uses.
                ctx.Writer.Line(source switch
                {
                    BoundFigurative fig => PlaceRenderer.WriteFill(rmp,
                        FigurativeConstants.FillText(fig.Kind, ctx.Data.Collating, rmp.Inner.Item.OperandPic?.Category, ctx.Data.NationalCollating)),
                    BoundAllLiteral all => PlaceRenderer.WriteFill(rmp, CsLiteral(all.Literal)),
                    _ => PlaceRenderer.Write(rmp, OperandText.AsString(source, num)),
                });
                break;
            case MoveKind.Group:
                EmitGroupMove(target, source);
                break;
            case MoveKind.GroupToElementary:
                EmitGroupToElementaryMove(target, source);
                break;
            case MoveKind.FigurativeToNumericImage:
            case MoveKind.Convert:
                // WHY this store may be a fill decides how an unstorable one is NAMED: the written
                // figurative is the pre-2023 residue §14.9.25.3 SR5 removed, the substituted one is
                // §14.9.25.4 GR2 acting on a zero-length literal the standard still permits. Blaming the
                // wrong rule in a loud is the misattribution kb/Work PB393 measured, one verb over — and the
                // provenance rides the STORE, because it is a bind-time fact and reading m.Source to recover
                // it is the scalar read this shape exists to remove.
                ctx.Writer.Line(ElementaryStore(target, source, origin));
                break;
        }
    }

    /// <summary>⛔ ISO §14.6.10 1) / Annex A.2 item 36 — operands that overlap have an UNDEFINED result, so what
    /// WiseOwl COBOL does is documented (docs/CONFORMANCE.md §3 D-OVL1/D-OVL2, kb/Work PB1907): the receiver is
    /// prepared BEFORE the sender is read, exactly as GnuCOBOL's <c>libcob</c> does. The decision was made at
    /// bind time (<see cref="MoveOverlap"/>, carried on <see cref="MoveStore.Prefill"/>) and applies only where
    /// the overlap is a compile-time fact, so every other MOVE emits nothing here and is unchanged. The store
    /// that follows reads the sender AFTER this statement, which is the whole of the rule.</summary>
    private void EmitOverlapPrefill(Place target, OverlapPrefill prefill)
    {
        switch (prefill.Kind)
        {
            case OverlapPrefillKind.LeadingSpaces:
                // The leading `Positions` positions only: the sender may overlap the rest, which still holds its
                // pre-move content when the store reads it.
                ctx.Writer.Line(PlaceRenderer.Write(target, RuntimeApi.StrWindowInto(
                    PlaceRenderer.Read(target), "1", $"{prefill.Positions}", CsLiteral(new string(' ', prefill.Positions)))));
                break;
            case OverlapPrefillKind.ZeroDigits:
                // libcob's memset of the receiver to the character '0': every position, the sign position of a SIGN
                // SEPARATE item included, and no operational sign on the last digit (an overpunch such as '{' would
                // read as a non-digit when the overlapping sender is parsed). An image-backed receiver takes that
                // image as it is; a native numeric field has no character image, and its zero is MOVE ZERO.
                ctx.Writer.Line(target is RedefViewPlace || target.Item.StoreAsImage
                    ? PlaceRenderer.Write(target, CsLiteral(new string('0', target.Item.ImageWidth)))
                    : ElementaryStore(target, new BoundFigurative('Z'), MoveSenderOrigin.Written));
                break;
        }
    }

    /// <summary>The ONE-STATEMENT store of an ELEMENTARY move — the <see cref="MoveKind.Convert"/> and
    /// <see cref="MoveKind.FigurativeToNumericImage"/> renderings, RETURNED rather than written so GR1's
    /// zero-length-item test can place BOTH of its arms inside one emitted <c>if/else</c> (kb/Work PB425)
    /// instead of the emitter growing a second copy of either rendering. The kind is re-asked of THIS sender
    /// because GR1's two arms differ in it: the substituted figurative into a numeric receiver is the image
    /// fill, the written sender is the conversion.</summary>
    /// <summary>Why a FILL is being stored into a numeric receiver — the rule to blame when the receiver has no
    /// character image to hold it (<see cref="FigurativeToNumericImageStore"/>'s narrow loud). Three provenances,
    /// three different rules, and only one of them is the removed construct.</summary>
    private static string FillCite(MoveSenderOrigin origin) => origin switch
    {
        MoveSenderOrigin.ZeroLengthLiteral =>
            "the figurative constant ISO §14.9.25.4 GR2/GR3 substitutes for a zero-length literal-1 (legal "
            + "source at every edition — §14.9.25.3 SR5 names only figurative constants written in the source)",
        MoveSenderOrigin.ZeroLengthItem =>
            "the figurative constant ISO §14.9.25.4 GR1 substitutes when identifier-1 is a zero-length item at "
            + "run time (§8.5.1.10), by way of GR2/GR3",
        _ => "an alphanumeric figurative constant — narrow pre-2023 residue of the move ISO 2023 removed, "
             + "§14.9.25.3 SR5 / Annex E.2 item 1",
    };

    private string ElementaryStore(Place target, BoundOperand source, MoveSenderOrigin origin) =>
        MoveClassifier.Kind(source, target) is MoveKind.FigurativeToNumericImage
            ? FigurativeToNumericImageStore(target, source, FillCite(origin))
            // An ANY LENGTH receiver stores at the CARRIER's current length (ISO §13.18.2 GR1 — the item is n
            // repetitions of its picture symbol, n = the activating argument's length).
            : PlaceRenderer.Write(target, ConvertSource(source, target.Item,
                target.Item.IsAnyLength ? ReceivingStore.AnyLengthWidth(target) : null));

    /// <summary>MOVE of an alphanumeric figurative constant (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) or an ALL
    /// "literal" containing a non-digit into an ELEMENTARY NUMERIC receiver — the PRE-REMOVAL semantics of a
    /// construct ISO/IEC 1989:2023 REMOVED (§14.9.25.3 SR5; Annex E.2 item 1 bullet 1 — permitted through 2014).
    /// The binder's "move-alphanumeric-figurative-removed-2023" gate carries the 0902 diagnostics; this emit path
    /// exists regardless, reachable at --std 85/2002/2014 and at 2023 --permissive. Provisional (ratified decision
    /// 1 — the legacy oracle is the interim authority for pre-2023 semantics of removed constructs): the fill
    /// CHARACTERS are deposited as the receiver's character image, repeated to its width (§8.3.3.6.4 GR2), exactly
    /// the legacy byte engine's fill — MOVE QUOTE TO PIC 9(3) leaves three quotation marks, IS NUMERIC is then
    /// false, and a later numeric read decodes deterministically (§14.6.13.2: a non-digit contributes no digit).
    /// The binder flagged an eligible numeric-DISPLAY receiver <see cref="DataItem.StoreAsImage"/> (REUSING the
    /// §14.9.25.4 MOVE GR4 whole-group image substrate — never a parallel mechanism), so the store is a plain image
    /// write; a Tier-B REDEFINES window / NumericImagePlace writes its character image directly. Only a
    /// non-DISPLAY numeric receiver (BINARY / PACKED / COMP-5 / float — no character image in the typed-native
    /// model) remains a NARROW loud guard (§1.4). Eligibility is the NODE's <see cref="MoveKind"/> (classified
    /// once — figurative ZERO and digit-only ALL classify as Convert, VALUE moves not fills).</summary>
    private string FigurativeToNumericImageStore(Place target, BoundOperand source, string fillCite)
    {
        // The KIND already decided eligibility (MoveClassifier — numeric receiver + S/Q/H/L figurative or
        // non-digit ALL); an unmatched shape here is a classifier/renderer drift bug, not a fallthrough.
        string image = source switch
        {
            BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } f =>
                $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating)}, {target.Item.ImageWidth})",
            BoundAllLiteral { IsDigitOnly: false } a =>
                CsLiteral(EmitText.RepeatToWidth(a.Literal, target.Item.ImageWidth)),
            _ => throw new InvalidOperationException(
                $"MoveKind.FigurativeToNumericImage with a non-fill source ({source.GetType().Name}) — "
                + "MoveClassifier and this renderer have drifted"),
        };
        return target.Item.StoreAsImage || target is RedefViewPlace or NumericImagePlace
            ? PlaceRenderer.Write(target, image)
            : LoudStmt($"MOVE of {fillCite} into the numeric item '{target.Item.CobolName}' without "
                + "image-backed storage (a BINARY/PACKED/COMP-5/float or Tier-A shared-storage receiver has no "
                + "character image to fill)");
    }

    // IsGroupSender lives on MoveClassifier since P7 Step 7 (the ONE GR4 sender-side test).

    /// <summary>MOVE from a GROUP sender into an ELEMENTARY receiver — a GROUP MOVE (ISO §14.9.25.4 GR4): treated
    /// "exactly as if it were an alphanumeric to alphanumeric elementary move, except that there is no conversion
    /// of data from one form of internal representation to another", the receiving area "filled without
    /// consideration for the individual elementary or group items" — NO numeric conversion (F1-16), NO editing
    /// into a numeric-edited or alphanumeric-edited mask (F1-38 / F1-20, F1-36; GR5 editing applies only to valid
    /// ELEMENTARY moves), NO de-editing. Alignment is §14.6.8 alphanumeric: left-justified, right space-fill /
    /// right truncation — and the receiver's JUSTIFIED still applies ("exactly as if … elementary move";
    /// §13.18.32 attaches to the receiver). The width-fitted area is then decoded into the receiver's own
    /// representation by THE ONE elementary storage-area store (<see cref="ReceivingStore.StorageArea"/> — the
    /// same decode a READ into an elementary record area rides, kb/Work PB1556): string-backed receivers take
    /// the bytes as they are; a native numeric receiver decodes them in its OWN byte form (zoned, radix-2, BCD or
    /// IEEE), deterministically for possibly-incompatible content as §14.6.13.2 permits.</summary>
    private void EmitGroupToElementaryMove(Place target, BoundOperand source)
    {
        var item = target.Item;
        // ⛔ ISO §14.9.25.4 GR8 FIRST — "If the sending or receiving item is a dynamic-length elementary item,
        // the current content of the dynamic-length elementary item is moved or changed as specified in
        // 8.5.1.10.4" — and §8.5.1.10.4: "the new value becomes the content of the item. The new length … is
        // determined by the length of new content." A dynamic-length receiver therefore takes the sending
        // GROUP's image WHOLE, with NO width fitting: it has no width to fit to (kb/Work PB393).
        // <para>This arm exists because the ONE dynamic-length store (<see cref="ConvertSource"/>'s
        // RuntimeApi.DynStore) is reached only by MoveKind.Convert, and a GROUP sender routes here instead —
        // the two-arm dispatch with one arm fixed. The other three kinds cannot reach a dynamic-length
        // receiver: MoveKind.Group's receiver is a group, FigurativeToNumericImage's is numeric (a
        // dynamic-length item is alphanumeric or national, §8.5.1.10.1), and a RefModSlice receiver is by
        // §8.5.1.10.4's own first sentence a FIXED-length item of the current length.</para>
        if (item.IsDynamicLength)
        {
            ctx.Writer.Line(PlaceRenderer.Write(target, ReceivingStore.Characters(item,
                OperandText.NonElementaryMoveSender(source, num, "group MOVE into"), "")));
            return;
        }
        if (!item.IsImageCapable)
        {
            ctx.Writer.Line(LoudStmt(TierCIsland.Reason(item, "group MOVE into")));
            return;
        }
        // The width-fitted image (§14.6.8), fitted in the receiver's STORAGE positions — the unit the sending
        // group's image is in. GR4's move is "an alphanumeric to alphanumeric elementary move, except that there
        // is no conversion of data from one form of internal representation to another", so the receiving AREA
        // takes the sending bytes as they are and the receiver's own representation is decoded FROM them, never
        // the sending characters re-encoded into it (kb/Work PB943's sibling sweep). For every DISPLAY-usage
        // receiver the two units coincide (ByteWidth IS ImageWidth: V occupies no position, SIGN SEPARATE adds
        // one, P adds none — §13.18.40). ⛔ For a USAGE NATIONAL receiver they do not — §13.18.60.4 GR8's uniform
        // size, pinned at two bytes by D-N1 — and for a USAGE BIT receiver the area is its ceil(n/8) packed bytes:
        // both used to take the group's BYTES as their CHARACTERS, so `MOVE AG TO PIC N(4)` stored the sending
        // characters widened (a conversion GR4 forbids) and `MOVE AG TO PIC 1(3) USAGE BIT` left the letter "A"
        // in a boolean carrier. An ANY LENGTH receiver's width exists only at runtime (§13.18.2 GR1 — the
        // carrier's current length, in its own positions).
        // ReceivingStore.AnyLengthWidth is the ONE spelling of an ANY LENGTH receiver's width (kb/Work PB979).
        string len = item.IsAnyLength ? ReceivingStore.AnyLengthWidth(target) : "";
        string gw = !item.IsAnyLength ? $"{item.ByteWidth}"
            : item.Pic is { Usage: Usage.National } ? $"({len} * {RuntimeApi.BytesPerNational})"
            : item.Pic is { Usage: Usage.Bit } ? $"(({len} + {BitLayout.BitsPerCharacter - 1}) / {BitLayout.BitsPerCharacter})"
            : len;
        string area = ReceivingStore.Characters(item, OperandText.NonElementaryMoveSender(source, num, "group MOVE into"), gw);
        // The receiver's representation is decoded FROM the area's bytes by THE ONE elementary storage-area
        // store — the same decode a READ into an elementary record area rides (kb/Work PB1556).
        ctx.Writer.Line(ReceivingStore.StorageArea(target, area, ReceivingStore.StoragePositions(target)));
    }

    /// <summary>MOVE into a whole group (alphanumeric semantics, ISO §14.9.25.4 MOVE GR4 — no conversion, filled without
    /// consideration for subordinate items): the source's character image fills the group's leaves via
    /// <c>FromImage</c>. Handles any image-capable group — alphanumeric, numeric-edited, numeric-DISPLAY (native or
    /// stored as its character image, <see cref="DataItem.StoreAsImage"/>), and BINARY/PACKED/COMP-5/float
    /// leaves (their image slices are the pinned byte representations — radix-2/BCD/IEEE, COBOLNET_DESIGN
    /// §14.4; kb/Work PB164 + R40 — INDEX leaves included). Only a variable-length group or a group with a
    /// pointer/object-class leaf stays the genuine Tier-C byte-island (deferred, loud).</summary>
    private void EmitGroupMove(Place target, BoundOperand source)
    {
        // ⛔ ISO §14.9.25.4 GR9 GOVERNS FIRST (kb/Work PB393) — "If both the sending operand and the receiving
        // data item are group items and one or both is a variable-length group, the following rules apply".
        // GR4's plain group move is explicitly the OTHER case ("Any move that is not an elementary move, AND
        // DOES NOT REFERENCE A VARIABLE-LENGTH GROUP, is treated exactly as if it were an alphanumeric to
        // alphanumeric elementary move"), so this test precedes every GR4 path below — including
        // AlignedLeafPairs, which for two identically-laid-out variable-length groups produced the RIGHT answer
        // by accident and thereby hid the hole (the shape a first test covers).
        if (VariableLengthGroupMove(target, source)) return;
        if (!target.Item.IsCharacterImage)
        {
            // A group MOVE is a content copy of the underlying representation, without conversion (ISO
            // §14.9.25.4 GR4 — both operands treated as elementary alphanumeric). When the source is a group
            // whose flattened leaf LAYOUT is positionally identical to the receiver's (same usage / digits /
            // scale / sign / width leaf-by-leaf — NC107A's all-COMP U5 → U9), that representation copy is
            // exactly a memberwise leaf copy — kept FIRST: under the digit-image representation both paths are
            // correct (GR4's representation copy ≡ memberwise copy for identical layouts), but the memberwise
            // path skips the encode/decode round trip and is the locked NC107A shape.
            if (source is BoundFieldOperand { Place.Item: { IsGroup: true } srcGroup }
                && AlignedLeafPairs(srcGroup, target.Item) is { } pairs
                && pairs.Select(p => (S: refs.ResolveItem(p.Src), T: refs.ResolveItem(p.Tgt))).ToList()
                    is { } resolved && resolved.All(r => r.S is not null && r.T is not null))
            {
                foreach (var (s, t) in resolved)
                    ctx.Writer.Line(PlaceRenderer.Write(t!, PlaceRenderer.Read(s!)));
                return;
            }
            // Non-aligned layouts (ST127A's 10-leaf WS twin → 11-leaf SD record) and class-view sources
            // (ST134A's SAME-RECORD-AREA window → SD record) fall through to the image path below: an
            // image-capable receiver distributes the source's character image via FromImage exactly like a
            // character group — its fixed-point leaves decode their zoned slices (GR4: filled without
            // consideration for the individual items, over the implementor digit-image representation). Only
            // the genuinely incapable receiver (a VARIABLE-LENGTH group or a group with a pointer/object-class leaf, kb/Work PB164 + R40) stays loud (§1.4).
            if (!target.ImageCapable)   // the OPERAND's capability — a dynamic-table ELEMENT has an image (kb/Work PB189)
            {
                ctx.Writer.Line(LoudStmt(TierCIsland.Reason(target.Item, "MOVE to group")));
                return;
            }
        }
        int width = target.Item.ImageWidth;
        // §14.9.25.4 GR4: a GROUP receiver makes the move NON-elementary — "treated as an alphanumeric to alphanumeric
        // elementary move, EXCEPT that there is no conversion of data from one form of internal representation to
        // another". GR6a's operational-sign drop applies only to valid ELEMENTARY moves, so a signed-numeric elementary
        // source keeps its DISPLAY overpunch image here (deSign:false — the sign-aware CobolNum.FormatDisplay), copied
        // verbatim into the group's leaves; this matches the already-correct GROUP-sender path (OperandText.AsImage
        // keeps the sign). A no-op for non-numeric / unsigned-numeric senders. (CA28.)
        // ALL "literal" repeats to the RECEIVER width (ISO §8.3.3.6.4 GR2) — space-padding it would fill the group's
        // tail leaves with blanks instead of the repeated pattern (NC243A's 7-dim table seed).
        string image = source is BoundFigurative f
            ? $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating)}, {width})"
            : source is BoundAllLiteral all
            ? CsLiteral(EmitText.RepeatToWidth(all.Literal, width))
            : RuntimeApi.StrStore(OperandText.NonElementaryMoveSender(source, num, "MOVE to group"), $"{width}");
        // The ONE group-image store (PlaceRenderer.WriteGroupImage — kb/Work PB70): the §13.18.38 GR8a current-extent
        // splice for an occurs-depending receiver with data-name-1 outside, the Tier-B view's window, the OCCURS
        // DYNAMIC receiving accessor, and the plain FromImage — written once for every verb that deposits an image.
        ctx.Writer.Line(PlaceRenderer.WriteGroupImage(target, image, "MOVE to group"));
    }

    /// <summary>⛔ THE ISO §14.9.25.4 GR9 MOVE — "If both the sending operand and the receiving data item are
    /// group items and one or both is a variable-length group" (kb/Work PB393). Returns false when GR9's own
    /// antecedent does not hold, so the caller falls through to GR4's plain group move.
    ///
    /// <para><b>GR9 is a STRUCTURAL walk over the two groups' §8.5.1.12 components, not a flat byte copy</b>, and
    /// the standard says so in its COMPARISON twin: §8.8.4.2.17 has the identical operation proceed "from left to
    /// right … except that — when corresponding tables are encountered, they are compared as described in
    /// 14.6.9.3 … — when corresponding dynamic-length elementary items are encountered, the length is determined
    /// as described in 8.5.1.10.4 … After comparison of corresponding tables or dynamic-length elementary items,
    /// comparison continues with the next data item in each of the compatible groups." GR9a's "corresponding
    /// character position" is therefore the position under §8.5.1.12.2's correspondence — the accounting in which
    /// a dynamic-length item is zero-length and a matched dynamic-capacity table is one element — and NOT the
    /// receiver's incidental current content lengths, which would make the result of a MOVE depend on what the
    /// receiving group happened to hold.</para>
    ///
    /// <para>That is exactly the carrier <c>CobolVarGroup</c> already models (kb/Work PB204): the collapsed fixed
    /// run plus each variable-length component's current content, in order. So GR9 IS
    /// <c>receiver.FromVarImage(sender.AsVarImage())</c>, and each of GR9's sub-rules is discharged by a part of
    /// that codec that was already written to the same clause:</para>
    /// <list type="bullet">
    ///   <item>GR9a's positional move of the non-table character positions — the fixed run, stored through the
    ///     ONE image-member law at §8.5.1.12.3 offsets.</item>
    ///   <item>GR9a's "the table in the sending group is moved to the corresponding table in the receiving group,
    ///     as specified in 14.6.9.2" — the component store (<c>FromCurrentImage</c>: recreate the receiving table
    ///     from the sending capacity; a non-dynamic receiver fits to its own occurrence count, §14.6.9.2 rules 1
    ///     and 2). The store moves element IMAGES; §14.6.9.2's "Correspondingly numbered elements are moved
    ///     according to the rules of the MOVE statement" is completed by the bound element moves
    ///     <see cref="EmitElementMoves"/> renders after it, for every elementary pair the image does not already
    ///     answer (kb/Work PB1144).</item>
    ///   <item>GR9b's excess part — the fixed run is width-fitted, which IGNORES a longer sender's excess and
    ///     SPACE-FILLS a shorter one's (step 3, "all other character positions are filled with space
    ///     characters"); step 1 ("if the data item to be space-filled is a dynamic-length elementary item, the
    ///     length of the receiving operand is set to zero") is the empty component a missing sender component
    ///     yields, stored through §8.5.1.10.4's replace-with-no-padding rule; step 2 is
    ///     <c>FromCurrentImage</c>'s own §14.6.9.4 space fill.</item>
    /// </list>
    ///
    /// <para>A FIXED-length group on either side is admitted by §8.5.1.12.1 ("only one of the operands may be a
    /// variable-length group") and decomposes into the SAME carrier through
    /// <c>VariableLengthCompatibility.CorrespondingSpans</c> + <c>CobolVarGroup.FromFixedImage</c> — §8.5.1.12.3
    /// sentence 3 and §14.6.9.1 both say to treat its table as a dynamic-capacity table of its fixed or DEPENDING
    /// count, so this is the standard's own conversion rather than an adapter invented here.</para>
    ///
    /// <para>The pair reached emit only after §14.9.25.3 SR9 passed at bind
    /// (<c>MoveTable16.Validity</c>, framed by <c>MoveBinder.MoveCategoryLegality</c>), so the two component sequences correspond one for
    /// one by construction — which is why an ordinal carrier is faithful here for the same reason it is at the
    /// activation boundary.</para></summary>
    private bool VariableLengthGroupMove(Place target, BoundOperand source)
    {
        // §14.9.25.4 GR9's antecedent: BOTH operands are group items AND one or both is a VARIABLE-LENGTH group. A
        // reference-modified operand is an ELEMENTARY alphanumeric item by rule (§8.4.3.3.4 GR6), never a group.
        // ⛔ A level-66 THROUGH alias IS a group item (§13.18.45.4 GR2), so it meets §14.9.25.4 GR9's first conjunct and is
        // asked as ItemCategory.IsGroupItem — the CATEGORY question — never the structural IsGroup, which it
        // fails for want of subordinate entries (kb/Work PB907; the bind-side twin is
        // MoveClassifier.VariableLengthGroupSender, which this method asks). It is never
        // VARIABLE-LENGTH (§13.18.45.3 SR8), so it takes the FIXED-group arm of PlaceRenderer.VarGroupCarrier / VarCarrierWrite:
        // its layout is its span (VariableLengthCompatibility.Layout), and its image is the composed RenamesPlace string (PlaceRenderer).
        // The antecedent is MoveClassifier's ONE reading, which the binder's §14.6.9.2 element moves ask too (kb/Work PB1144).
        if (MoveClassifier.VariableLengthGroupSender(source, target) is not { } send) return false;
        ctx.Writer.Line(PlaceRenderer.VarGroupCarrier(send, target.Item, "the sending variable-length group",
                "the sending group of a variable-length group MOVE") is not { } carrier
            ? LoudStmt(VarShapeReason(send.Item, "the sending operand of a variable-length group MOVE"))
            : VarCarrierWrite(target, send.Item, carrier) ?? LoudStmt(
                VarShapeReason(target.Item, "the receiving operand of a variable-length group MOVE")));
        return true;
    }

    /// <summary>The RECEIVING half of <see cref="PlaceRenderer.VarGroupCarrier"/> (the ONE sending-side reader, shared
    /// with the §8.8.4.2.17 comparison — kb/Work PB1467) — the statement that distributes the carrier back into the
    /// receiving group's members. Null when the shape cannot be distributed.</summary>
    private static string? VarCarrierWrite(Place g, DataItem other, string carrier) =>
        VariableLengthCompatibility.IsVariableLength(g.Item)
            ? g.Item.CurrentExtentImageCapable
                ? PlaceRenderer.WriteVarGroupImage(g, carrier, "the receiving variable-length group")
                : null
            : VariableLengthCompatibility.CorrespondingSpans(g.Item, other) is { } spans && g.ImageCapable
                ? PlaceRenderer.WriteGroupImage(g,
                    RuntimeApi.VarGroupToFixedImage(carrier, g.Item.ImageWidth, PlaceRenderer.SpanArray(spans)),
                    "the receiving group of a variable-length group MOVE")
                : null;

    /// <summary>Why a GR9 operand's §8.5.1.12 components cannot be composed by THIS implementation — a named
    /// shape, never the generic Tier-C string, because the generic one blames "a dynamic-length /
    /// dynamic-capacity member" and a FIXED group operand has none (the misattribution kb/Work PB393 measured).
    /// Both residues are the pre-existing current-extent boundary published as CONFORMANCE.md A.1 item 57, not
    /// something GR9 introduces.</summary>
    private static string VarShapeReason(DataItem g, string role) =>
        VariableLengthCompatibility.IsVariableLength(g)
            ? $"{role}, '{g.CobolName ?? g.CsName}', is a variable-length group whose current extent this "
              + "implementation cannot compose (an OCCURS DEPENDING member, or a runtime-length item inside a "
              + "table element) — ISO §14.9.25.4 GR9 over §8.5.1.12; CONFORMANCE.md A.1 item 57"
            : $"{role}, '{g.CobolName ?? g.CsName}', is a fixed-length group whose record image this "
              + "implementation cannot lay out in character positions (a USAGE BIT leaf, or a leaf with no "
              + "character image) — ISO §14.9.25.4 GR9 over §8.5.1.12.3; COBOLNET_DESIGN §4.2";

    /// <summary>The positionally-paired leaves of two groups whose flattened layouts are IDENTICAL — each pair
    /// shares usage, digit count, scale, sign and storage shape, and neither group involves OCCURS, REDEFINES,
    /// RENAMES or reference ambiguity (so <see cref="ReferenceResolver.ResolveItem"/> is exact). Null when the
    /// layouts differ — the caller falls back to the Tier-C loud guard. (ISO §14.9.25.4 GR4: the group move
    /// copies the underlying representation; identical layouts make that a memberwise copy.)</summary>
    private static List<(DataItem Src, DataItem Tgt)>? AlignedLeafPairs(DataItem source, DataItem target)
    {
        // An OCCURS anywhere on the ancestor chain means the group reference is an ELEMENT (needs subscripts
        // this item-level resolution cannot supply) — not this fast path.
        for (var a = source; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        for (var a = target; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        var src = new List<DataItem>();
        var tgt = new List<DataItem>();
        if (!Flatten(source, src) || !Flatten(target, tgt) || src.Count != tgt.Count || src.Count == 0)
            return null;
        for (int i = 0; i < src.Count; i++)
        {
            var (a, b) = (src[i].Pic!, tgt[i].Pic!);
            if (a.Usage != b.Usage || a.Category != b.Category || a.Digits != b.Digits || a.Scale != b.Scale
                || a.Signed != b.Signed || a.Length != b.Length || src[i].StoreAsImage != tgt[i].StoreAsImage)
                return null;
        }
        return src.Zip(tgt).ToList();

        // Declaration-order leaves; false when the subtree has a shape the pairing cannot prove equivalent.
        static bool Flatten(DataItem item, List<DataItem> leaves)
        {
            if (item.Occurs is not null || item.RedefinesTargetName is not null
                || item.Renames66.Count > 0 || item.Class is not null)
                return false;
            if (!item.IsGroup)
            {
                if (item.Pic is null) return false;
                leaves.Add(item);
                return true;
            }
            foreach (var c in item.Children)
                if (!Flatten(c, leaves))
                    return false;
            return true;
        }
    }

    /// <summary>The compile-time numeric value of a digit-only <c>ALL "literal"</c> moving to a numeric receiver
    /// (ISO §8.3.3.6.4 GR2 repetition + §14.9.25.4 GR6d3b — the digit string spans the receiver's digit positions,
    /// fraction digits included, so the unscaled value IS the repeated digit run at the receiver's scale). ≤18
    /// digit positions fold to a native <c>long</c> literal; a WIDE receiver (19–31 digits, COBOL-2002+ — ISO
    /// §8.3.3.3.2) decodes its digit run through the ONE deterministic digit decode (<c>FromAlphanumeric</c>,
    /// Int128 — numeric design D1).</summary>
    private static NumX AllDigitFill(string literal, PicInfo pic)
    {
        string digits = EmitText.RepeatToWidth(literal, Math.Max(pic.Digits, 1));
        return digits.Length <= 18
            ? new NumX($"{long.Parse(digits, System.Globalization.CultureInfo.InvariantCulture)}L", pic.Scale)
            : new NumX(RuntimeApi.NumDigitMagnitude(CsLiteral(digits)), pic.Scale);
    }

    /// <summary>True when a MOVE source carries a NUMERIC VALUE for the §14.9.25.4 GR5 editing path into a
    /// numeric-edited receiver: a numeric literal/expression, figurative ZERO, a numeric data item, OR a
    /// numeric-EDITED data item (GR5 + GR6d1 — a numeric-edited sender is DE-EDITED to its numeric value, which may be
    /// signed, before being re-edited into the receiver mask). A plain alphanumeric source is NOT numeric — it moves
    /// as characters (the §14.9.25.3 Table-16 unsigned-integer edit at the fall-through arm).</summary>
    private static bool IsNumericOperand(BoundOperand source) => source switch
    {
        BoundNumericLiteral or BoundComputedOperand => true,
        BoundFigurative { Kind: 'Z' } => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Numeric or PicCategory.NumericEdited,
        _ => false,
    };

    /// <summary>The C# expression a MOVE source converts to when stored into <paramref name="target"/>.
    /// <paramref name="runtimeWidth"/> — the receiver's RUNTIME character count expression for an ANY LENGTH
    /// receiver (ISO §13.18.2 GR1: the item behaves as n repetitions of its picture symbol where n is the
    /// activating argument's length, so a MOVE stores at the CARRIER's current length, never Pic.Length=1);
    /// null (every other receiver) keeps the compile-time width.</summary>
    /// <param name="rounding">The ROUNDING MODE of the transfer. A MOVE never passes it — §14.6.8.2 r4's
    /// alignment truncates by rule — and the default therefore preserves that. The ONE caller that does is the
    /// REPORT SECTION's implicit COMPUTE: §13.18.53.4 GR2 ("If the ROUNDED phrase is specified, the implicit
    /// COMPUTE statement has the corresponding ROUNDED phrase") and §13.18.54.4 GR4 ("the content of the sum
    /// counter is computed according to the general rules for the COMPUTE statement with the ROUNDED phrase").
    /// A rounded implicit transfer is still THIS conversion — the receiver's PICTURE governs the store either
    /// way — so the mode is threaded rather than a second conversion written (kb/Work PB852).</param>
    public string ConvertSource(BoundOperand source, DataItem target, string? runtimeWidth = null,
        CobolRounding rounding = CobolRounding.Truncation)
    {
        var pic = target.OperandPic!;   // an elementary receiver's picture, or a bit / national group's as-if picture (D20/PB79)
        // A DYNAMIC LENGTH receiver (ISO §8.5.1.10.4 / §13.18.19): store the sender's DISPLAY image, replacing the
        // old content, with the new length = the SENDING length TRUNCATED ON THE RIGHT to the LIMIT and NO padding
        // (the minimum length is zero, §13.18.19.4 GR1). A figurative constant OTHER THAN `ALL literal` has a
        // sending length of ONE character (§8.3.3.6.4 GR3b) — so MOVE SPACE/ZERO/HIGH-VALUE/… each store a single
        // fill character (length 1), never length 0; a zero-length literal "" is a BoundStringLiteral whose AsString
        // is "" (length 0 — honoring §14.9.25.4 GR2's NOT-SPACE-substituted rule for the dynamic-length receiver).
        // AsString already renders every source correctly (a figurative as new string(fill,1)), so the general
        // DynStore path is exhaustive. A PIC X/N dynamic-length item carries no edit mask, so this precedes the
        // fixed-width figurative/ALL and numeric-edited paths below.
        if (target.IsDynamicLength)   // the ONE elementary character receiving store owns §8.5.1.10.4 (kb/Work PB871)
            return ReceivingStore.Characters(target, OperandText.AsString(source, num, deSign: true), "");
        string wN = runtimeWidth ?? pic.Length.ToString();   // the string-category store width (§13.18.2 GR1)
        // ⛔ A WHOLE-WIDTH FILL GOES THROUGH THE EDITOR, NEVER AROUND IT, when the receiver edits. §8.3.3.6.4 GR2
        // and §14.9.25.4 GR6 are two different steps and both apply: the figurative / ALL-literal source is first
        // repeated "until the size of the resultant string is greater than or equal to the number of character
        // positions in the associated data item", and THEN the move performs "any editing specified for … the
        // receiving data item", so an INSERTION position still takes its own character — `MOVE SPACES TO
        // PIC XX/XX` yields `  /  ` (NIST NC223A INI-TEST-GF-1) and `MOVE ALL "X"` yields `XX/XX`. The two guards
        // below therefore share ONE predicate, PicInfo.IsCharacterEdited, and defer to the switch's edited arm.
        // (kb/Work PB492: the figurative guard named only the ALPHANUMERIC half — measured `MOVE SPACE TO
        // PIC NN0NN` → five spaces where the zero must survive — and the ALL-literal guard named neither, so
        // `MOVE ALL "X" TO PIC XX/XX` stored `XXXXX`. One rule, two call sites, wrong at both.)
        // The OTHER exception is figurative ZERO into a numeric-edited receiver, which is the numeric value 0
        // EDITED into the mask ('ZZ9.99' shows '  0.00', not a zero-fill).
        if (source is BoundFigurative f && pic.Category is not PicCategory.Numeric
            && !(f.Kind is 'Z' && pic.Category is PicCategory.NumericEdited) && !pic.IsCharacterEdited)
            // Category-aware fill: a national/boolean receiver's HIGH/LOW-VALUE reads its OWN sequence — the
            // explicit national PCS extremes when declared, else the D-N3 pin — never the ALPHANUMERIC
            // program collating sequence's extreme (§8.3.3.6 GR6/GR7 over the national sequence).
            return $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating)}, {wN})";
        // ALL "literal" repeats the literal to the receiver width (ISO §8.3.3.6.4 GR2). An ANY LENGTH
        // receiver's width exists only at runtime — repeat at runtime, then width-fit (§13.18.2 GR1).
        if (source is BoundAllLiteral a && pic.Category is not PicCategory.Numeric && !pic.IsCharacterEdited)
            return runtimeWidth is null
                ? CsLiteral(EmitText.RepeatToWidth(a.Literal, pic.Length))
                : RuntimeApi.StrStore(RuntimeApi.StrRepeat(CsLiteral(a.Literal), runtimeWidth), runtimeWidth);

        // ⭐ THE EDITED CHARACTER CATEGORIES — alphanumeric-edited and national-edited — take ONE arm, ABOVE the
        // per-category switch, because §13.18.40.5 Table 7 gives them ONE type of editing between them ("Simple
        // insertion") and nothing else. Hoisting it here rather than writing a `case` per category is the point:
        // a case list is what the ACCEPT, STRING and MOVE arms each grew with only the alphanumeric half in it,
        // leaving a national-edited receiver with no renderer at all (kb/Work PB492). ISO §14.9.25.4 GR6 makes
        // the editing part of every valid elementary move, and GR6 a) the alignment: "When an alphanumeric,
        // alphanumeric-edited, national, or national-edited data item is a receiving operand, alignment and any
        // necessary space filling shall take place as defined in 14.6.8".
        if (pic.IsCharacterEdited)
        {
            // A figurative or ALL-literal source supplies the fill §8.3.3.6.4 GR2 computes — repeated to the
            // receiver's size — as the EDIT's sending characters, which the data positions then consume in
            // order while each insertion position takes its own character. The figurative fill is the
            // CATEGORY-AWARE one, so a national-edited receiver's HIGH/LOW-VALUE reads the national sequence
            // (§8.3.3.6 GR6/GR7), exactly as the whole-width guard above does.
            string aeSrc = source switch
            {
                BoundFigurative ff =>
                    $"new string({FigurativeConstants.Fill(ff.Kind, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating)}, {pic.Length})",
                BoundAllLiteral al => CsLiteral(EmitText.RepeatToWidth(al.Literal, pic.Length)),
                _ => OperandText.AsString(source, num, deSign: true),
            };
            // The mask AND the item's EDITING rules come from the one PicInfo — never a call-site mask deref
            // (kb/Work PB490: all three edited-character emit sites dropped PicInfo.EditingRules).
            return RuntimeApi.EditFormatSimpleInsertion(aeSrc, pic);
        }

        switch (pic.Category)
        {
            // A NUMERIC source moving to a numeric-edited receiver is EDITED into the receiver's picture
            // (ISO §14.9.25.4 GR6 — "any editing specified for … the receiving data item"; GR6 a) alignment);
            // an alphanumeric source stays a plain character move.
            case PicCategory.NumericEdited when IsNumericOperand(source):
                NumX e = num.AsNum(source, SenderContext(target), SendingRef.MoveToNumeric);
                // A FLOATING-POINT numeric-edited receiver (D21/PB66) takes the sender's EXACT form — no alignment to a
                // mask scale (it has none); the dispatch is RuntimeApi.EditFormatFor's, keyed on the receiver's picture.
                if (pic.IsFloatEdited)
                    return RuntimeApi.EditFormatFor(pic, e, "", "", ArithmeticEmitter.BwzFlag(target) + ctx.EditCfg(pic));
                // A float (Real) source lands into the edited receiver via the runtime's ToScaled at the RECEIVER's
                // fraction scale (MOVE truncates toward zero, §14.6.8.2) — the edit Format takes a scaled Int128,
                // not a double (D16 review: the numeric-edited path was missed by the Real integration → CS1503).
                // NB the receiver scale is the ONE PicInfo.ReceiverScale rule, NOT pic.Scale (a masked item's Scale is 0 —
                // the point is in the mask; a format-2 LOCALE item's IS pic.Scale — kb/Work PB64 T6).
                int ems = pic.ReceiverScale(ctx.Data.DecimalPointIsComma);
                // A STANDARD-DECIMAL intermediate lands at the receiver's scale (the §14.7 final transfer — the same
                // form ArithmeticEmitter's edited path uses; fix-queue PB65: MOVE FUNCTION E under the mode handed
                // the CobolDec to the Int128 edit path, CS1503 on conforming source).
                string editVal = e.Real ? RuntimeApi.FloatToScaled(e.Expr, $"{ems}", rounding, checkedLanding: false)   // a MOVE: §14.6.8.2 r4 truncation, low-order digits past the carrier (kb/Work PB77)
                    : e.Dec ? RuntimeApi.DecToUnscaled(e.Expr, $"{ems}", rounding)
                    : e.Expr;
                int editScale = e.Real || e.Dec ? ems : e.Scale;
                // The form dispatch (mask vs LOCALE) is EditFormatFor's — never a call-site EditMask deref.
                return RuntimeApi.EditFormatFor(pic, e, editVal, $"{editScale}",
                    ArithmeticEmitter.BwzFlag(target) + ctx.EditCfg(pic));
            // An ELEMENTARY ALPHANUMERIC source into a numeric-edited receiver IS a legal move (§14.9.25.3
            // Table 16): the sending characters are treated as an unsigned integer and EDITED into the mask
            // (§14.9.25.4 GR5 — NC104A MOVE-TEST-F1-39: "12345" → $12,345.00), never a plain character copy.
            // (A GROUP sender never reaches here — GR4 makes that a group move, no editing: EmitGroupToElementaryMove.)
            case PicCategory.NumericEdited:
            {
                string unsignedInt = RuntimeApi.NumFromAlphanumeric(OperandText.AsString(source, num, deSign: true), sending: true);
                return RuntimeApi.EditFormatFor(pic, new NumX(unsignedInt, 0), unsignedInt, "0",
                    ArithmeticEmitter.BwzFlag(target) + ctx.EditCfg(pic));
            }
            case PicCategory.Alphanumeric:
                // A signed numeric source drops its operational sign into an alphanumeric receiver (ISO §14.9.25.4 GR6a);
                // a JUSTIFIED receiver right-justifies (left space-fill / left truncation, §14.9.25.4 GR6c).
                // wN: an ANY LENGTH receiver stores at its runtime length (§13.18.2 GR1), else Pic.Length.
                return ReceivingStore.Characters(target, OperandText.AsString(source, num, deSign: true), wN);
            // A NATIONAL receiver stores exactly like alphanumeric on the character substrate (§14.6.8.5 —
            // left-justify, national-space pad, right truncation; JUSTIFIED per §13.18.32): A→N widening,
            // N→N, 9→N digit imaging, and boolean→N all ride AsString under the D-N4 Latin-1 identity
            // correspondence (§14.9.25.4 GR6/GR6a).
            case PicCategory.National:
                return ReceivingStore.Characters(target, OperandText.AsString(source, num, deSign: true), wN);
            // A BOOLEAN receiver pads/left-fills with boolean ZEROS (§14.6.8.6; JUSTIFIED §13.18.32 GR2).
            // Figurative ZERO already early-returned above as a '0' fill; the SR7-illegal figurative shapes
            // never reach emit (bind-rejected, MoveCategoryLegality).
            case PicCategory.Boolean:
                return RuntimeApi.StrStoreBoolean(OperandText.AsString(source, num, deSign: true), wN, target.Justified);
            case PicCategory.Numeric:
                // A digit-only ALL "literal" repeats across the RECEIVER's digit positions (ISO §8.3.3.6.4 GR2 —
                // repetition to the associated item's size, truncated from the right; §14.9.25.4 GR6d3b — a
                // figurative sending operand takes the receiver's digit count, fraction digits included), so
                // every digit position holds the pattern: ALL "5" → PIC 9(3) stores 555; → PIC 9V9 stores 5.5
                // (unscaled 55 at scale 1 — legacy-oracle confirmed). Valid at EVERY edition for the
                // single-digit-ALL → integer case (§14.9.25.3 SR5, obsolete-flagged 0903 at 2023 by the binder);
                // the non-integer / multi-character shapes are 0902-gated at 2023 and keep these pre-removal
                // semantics at 85/2002/2014 and under --permissive (ALL "57" → PIC 9(3) stores 575, the legacy
                // oracle's '85-obsolete-element behavior — provisional, ratified decision 1). This was the
                // BoundAllLiteral runtime-loud latent bug (W2 track A): the move compiled then died in AsNum.
                // A float RECEIVER (COMP-1/2/FLOAT-*, D16) holds the algebraic value in a native float/double —
                // no PICTURE, no scaled-integer store, no SIZE ERROR (an arithmetic ±Inf is a valid value; §14.6.8.3
                // GR1). §14.6.13.2 dash-3: the source read is EC-DATA-NOT-FINITE-EXEMPT only when sending and receiving
                // share the SAME floating-point usage specification (a verbatim copy, endianness aside — Usage enum
                // equality; endianness is a separate phrase, not a Usage value). A fixed source or a DIFFERENT float
                // usage (COMP-1↔COMP-2, and the reachable narrowing) keeps the CobolFloat.Sending wrap.
                if (pic.IsFloat)
                {
                    bool sameUsage = source is BoundFieldOperand fsrc
                        && fsrc.Place.Item.Pic is { IsFloat: true } sp && sp.Usage == pic.Usage;
                    // ⛔ A SAME-USAGE FLOAT MOVE IS A COPY, NOT A CONVERSION (kb/Work PB961). §14.9.25.4 GR6 c):
                    // "When the receiving data item is described with the same usage specification as the sending
                    // operand, the data in the sending operand is transferred to the receiving data item without
                    // change" — NOTE 3 names NaN representations as what that preserves. The algebraic path below
                    // widens a binary32 sender to binary64, which QUIETS a signaling NaN, so the copy reads the
                    // sender on its OWN carrier (a float stays a float end to end) and re-encodes only the
                    // receiver's endianness, which is the one change GR6 c) permits.
                    if (sameUsage)
                    {
                        string raw = NumericRenderer.FloatCarrierRead(((BoundFieldOperand)source).Place, SendingRef.SameUsageMove);
                        return target.StoreAsImage ? RuntimeApi.NumFormatImageFloat(raw, target.ProfileName, pic.IsSingle) : raw;
                    }
                    NumX fsrcNum = num.AsNum(source, ReceiverContext.None, SendingRef.MoveToNumeric);
                    // §14.9.25.4 GR6 d)4.a: "If the algebraic value of the sending operand is farther from zero than
                    // is permitted by the usage specifications of the receiving data item, the EC-DATA-OVERFLOW
                    // exception condition is set to exist" — a FATAL condition (Table 13), MOVE-only, armed by
                    // EcBinder. THE TEST IS ON THE ALGEBRAIC VALUE, so a decimal sender is handed over WHOLE:
                    // ⛔ an SDIDI sender reaches ±9.999…E+6144 and collapses to ±Infinity in ToDouble, so BOTH arms
                    // used to miss it (kb/Work PB271) — the double arm was a bare cast whose stated premise, "a
                    // double receiver cannot overflow from a finite double", quietly assumed a finite DOUBLE ever
                    // reached it, and StoreSingleChecked's own finite-source guard was already false.
                    // `MOVE 1.0E+400 TO F` stored +Infinity with no condition raised, at every checking level.
                    // A non-decimal sender cannot exceed binary64 (a fixed-point item caps at 31 digits, a float
                    // item is already binary64), so it keeps the double-domain single-precision check.
                    string fval = fsrcNum.Dec
                        ? $"({pic.ClrType})({RuntimeApi.FloatStoreDecChecked(fsrcNum.Expr, pic.IsSingle)})"
                        : pic.IsSingle
                            ? fsrcNum.Real
                                ? RuntimeApi.FloatStoreSingleChecked(NumericRenderer.Real(fsrcNum))
                                // A fixed-point sender converts to binary32 in ONE rounding (kb/Work PB1110): narrowing its
                                // binary64 is a second one, and a value just above a binary32 midpoint lands on the midpoint.
                                : RuntimeApi.FloatStoreScaledSingleChecked(NumericRenderer.ScaledSingle(fsrcNum))
                            : $"({pic.ClrType})({NumericRenderer.Real(fsrcNum)})";
                    // A WINDOWED float receiver (Tier-B / image-stored — the Step D arm-1 dissolution) re-encodes
                    // as its IEEE window bytes.
                    return target.StoreAsImage ? RuntimeApi.NumFormatImageFloat(fval, target.ProfileName, pic.IsSingle) : fval;
                }
                NumX n = source is BoundAllLiteral { IsDigitOnly: true } allDigit
                    ? AllDigitFill(allDigit.Literal, pic)
                    : num.AsNum(source, SenderContext(target), SendingRef.MoveToNumeric);
                // A float SOURCE lands into the fixed receiver via the runtime's ToScaled at the receiver scale (MOVE
                // truncates toward zero — §14.6.8.2 GR2/GR4 implementor-defined) then the ordinary store funnel
                // (rescale identity ⇒ no double-rounding; the digit-capacity + SIZE ERROR check still applies).
                int recvScaleM = target.OperandPic!.Scale;
                // A STANDARD-DECIMAL intermediate stores through the SDIDI Store overload (the §14.7 final
                // transfer), exactly as the arithmetic path does — fix-queue PB65: the MOVE arm was the one
                // numeric consumer without the Dec case, so MOVE FUNCTION E was a backend CS1503.
                // …and every carrier stores through the ONE store (NumericRenderer.StoreExpr — kb/Work PB84), so
                // the Dec/float/native switch is written once for MOVE, the arithmetic store and INVOKE alike.
                string stored = ArithmeticEmitter.Narrow(NumericRenderer.StoreExpr(n, recvScaleM, target.ProfileName, rounding), target);
                // A whole-group-aliased numeric-DISPLAY receiver stores its character image, not the raw long.
                return target.StoreAsImage ? RuntimeApi.NumFormatImage(stored, target.ProfileName) : stored;
            default:
                return "default";
        }
    }

    /// <summary>The RECEIVER-SCALE context a numeric MOVE source renders under (fix-queue PB60 /
    /// RV-15.68.4-1 half 2). <c>Receiverless</c> STAYS TRUE — the float family keeps its deliberate binary64
    /// sending value (only a genuinely receiver-less render keeps a float in binary64, per
    /// <see cref="ReceiverContext"/>'s own doc; MOVE's float landing owns the <c>ToScaled</c> at the receiver
    /// scale) — but the SCALE and the Int128-headroom cap now carry the ACTUAL receiver, so an exact-family
    /// intrinsic source renders at the precision the receiver can store instead of the receiver-less floor.
    /// Measured: <c>MOVE FUNCTION NUMVAL-C("$0.123456789") TO PIC S9(4)V9(9)</c> stored 0.123456000 while
    /// COMPUTE stored the exact value — §15.68.4 r1 carries no channel qualification. The scale mirrors
    /// <c>ArithmeticEmitter.ScaleOf</c> (a numeric-edited receiver's scale is its MASK's) and the digits
    /// <c>ArithmeticEmitter.IntDigitsOf</c> (from <c>DigitPositions</c>, never the '9' count — under-counting
    /// breaks the PB13 saturation-is-visible invariant).</summary>
    private ReceiverContext SenderContext(DataItem target)
    {
        if (target.Pic is not { } pic) return ReceiverContext.None;
        // The ONE receiver-scale rule (RuntimeApi.ReceiverScaleOf — PB64 T6: this copy and ArithmeticEmitter's
        // both fell to pic.Scale = 0 for a format-2 LOCALE item, silently truncating a fractional sender).
        int scale = pic.ReceiverScale(ctx.Data.DecimalPointIsComma);
        return ReceiverContext.None with { Scale = scale, IntegerDigits = Math.Max(0, pic.DigitPositions - scale), MoveSender = true };
    }
}
