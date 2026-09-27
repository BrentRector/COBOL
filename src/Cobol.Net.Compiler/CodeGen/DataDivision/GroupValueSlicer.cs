// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>⛔ THE §13.18.63.4 GR5 GROUP-VALUE AREA, <b>IN THE UNIT THE GROUP'S OWN STORAGE IS MEASURED IN</b> —
/// <paramref name="Text"/> is the area's contents and <paramref name="Bits"/> says whether those are BOOLEAN
/// POSITIONS (a bit group, §13.18.29.4 GR1b) or CHARACTER positions (every other group).
///
/// <para>The unit travels WITH the area rather than being re-derived by each consumer, because the two consumers
/// — the record-struct lane (<see cref="GroupValueSlicer.ComposedInit"/>) and the character-image lane
/// (<see cref="GroupImageCodec.ImageInitOf"/>) — must never disagree about it. Restating "is this a bit group?"
/// at a call site is how <c>GroupImageCodec.ImageInitOfOne</c> came to carry a NARROWER copy of the previous
/// exclusion (kb/Work PB207); one producer, one answer.</para></summary>
internal readonly record struct GroupArea(string Text, bool Bits);

/// <summary>The group-VALUE positional distributor (P7 Step 9l; ISO §13.18.63): a GROUP-level VALUE
/// initializes the whole area as ONE value of the group's own category, sliced positionally over the
/// (distributable) subtree at compile time; otherwise the member-wise composed initializer. Wired by
/// <see cref="DataEmitter"/>.</summary>
internal sealed class GroupValueSlicer(EmitContext ctx, PhysicalModel phys)
{
    /// <param name="subs">The OCCURRENCE CONTEXT — the subscripts of every OCCURS level entered on the way down
    /// from the record root (see <see cref="ValueInitializer.FieldInit"/>). It selects WHICH occurrence's
    /// group-level VALUE this composition deposits (§13.18.63.4 GR12–GR15 through <see cref="DataItem.ValueAt"/>)
    /// and travels on to the members, whose own table VALUEs are keyed by the same tuple.</param>
    /// <param name="recipe">Under <see cref="SeedRecipe.Initialize"/> a group-level VALUE is not applied: §14.9.20.4
    /// GR5 c) 1. b qualifies only "a data-item format VALUE clause … specified in the data description entry of the
    /// elementary data item", so the members compose one by one (kb/Work PB1267).</param>
    public string ComposedInit(DataItem group, Subscripts subs = default, SeedRecipe recipe = SeedRecipe.InitialState)
    {
        // A GROUP-level VALUE initializes the whole AREA (ISO §13.18.63.4 GR5) — the ONE area rule lives in
        // AreaOf, which also says which UNIT the area is measured in; here it distributes over the subordinate
        // items POSITIONALLY at compile time (NC104A `01 MOVE29A VALUE "$123.45". 02 MOVE30 PIC $999.99.`).
        // Distribution requires every leaf string-stored and no shared-storage member in the subtree; anything
        // else keeps the member-wise default (a SHARED-STORAGE subtree is not a loss — its Tier-B / EXTERNAL /
        // BASED backing is seeded by GroupImageCodec.ImageInitOf, which applies the SAME AreaOf rule to the
        // same group).
        if (recipe is SeedRecipe.InitialState && AreaOf(group, ctx, subs) is { } area && DistributableSubtree(group))
            return area.Bits ? SliceBitInit(group, area.Text) : SliceInit(group, area.Text);
        var parts = phys.PhysicalChildrenOf(group, subs, recipe).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>⛔ THE §13.18.63.4 GR5 GROUP-VALUE AREA RULE — the one place that says what a group-level VALUE
    /// puts in the group's storage, ridden by BOTH initializer lanes: this slicer (the typed-native record-struct
    /// fields) and <see cref="GroupImageCodec.ImageInitOf"/> (the character-image backings — Tier-B REDEFINES,
    /// EXTERNAL cells, BASED records, OO cells). Returns the area's contents PLUS its unit
    /// (<see cref="GroupArea"/>), padded / right-truncated to the group's own width per GR7, or null when the
    /// entry carries no VALUE or an operand form that is not an area fill.
    ///
    /// <para>GR5 is "the group area is initialized without consideration for the individual elementary or group
    /// items contained within this group" — so the deposit is positional and the members are whatever their own
    /// descriptions make of the positions underneath them. For an ALPHANUMERIC group that reading needs no
    /// character/byte reconciliation, because §13.18.63.3 SR14 requires every data item subordinate to an
    /// alphanumeric group item carrying a VALUE to be usage DISPLAY (screened by
    /// <c>DataBinder.CheckGroupValueDeclarations</c>, COBOLNET1702): the character image IS the byte image for
    /// that whole population.</para>
    ///
    /// <para><b>⛔ TWO UNITS, because a BIT GROUP's area is not made of characters.</b> §13.18.29.4 GR1a makes a
    /// GROUP-USAGE BIT group "a bit group and also a bit data item; its class and category are boolean", and GR1b
    /// describes it "as though it were an elementary data item of usage bit and class and category boolean
    /// described with PICTURE 1(m), where m is the bit length of the group" — so its area is <b>m boolean
    /// positions</b> laid out by the §8.5.1.6.3 walk, NOT the <c>ceil(m/8)</c> characters
    /// <see cref="DataItem.ImageWidth"/> reports. The two are different numbers and the members do not tile the
    /// character count: <c>01 BG GROUP-USAGE BIT VALUE B"1010". 05 B1 PIC 1(2). 05 B2 PIC 1(2).</c> is 4 bits =
    /// ONE character while B1 and B2 occupy <c>ceil(2/8)</c> = one character EACH. A positional CHARACTER slice
    /// over that group ran off the end of the area (an unhandled <c>ArgumentOutOfRangeException</c> out of
    /// <see cref="SliceInit"/>) and its single-member twin stored ONE boolean position where the literal has
    /// four — kb/Work PB207, which this method now answers instead of refusing.</para>
    ///
    /// <para><b>THREE operand forms, all of them area fills</b> (kb/Work PB184's sweep — the figurative arm was
    /// MISSING and it was a live silent wrong answer: `01 GZ VALUE ZEROS. 05 Z1 PIC X(2). 05 Z2 PIC X(2).`
    /// measured as four SPACES, where GR5 requires four '0' characters, because the group fell through to the
    /// member-wise default and each member took its own category default). §8.3.3.6.4 GR2 is the ONE rule for
    /// the repeat: it is the branch for the case "the length of the string is specified in the rules for the
    /// context", it names the VALUE clause explicitly, and it gives both the repeat-to-width and the
    /// truncate-from-the-right this method implements. (GR3 is the complementary branch — the length NOT
    /// specified — and its sub-rule a is scoped to a concatenation expression; neither governs here, because
    /// §13.18.63.4 GR5 makes the group area the receiving field and so specifies the length.)</para>
    /// <list type="number">
    ///   <item>a FIGURATIVE constant word, with or without ALL — its one character repeated to the group's width
    ///     (§8.3.3.6.4 GR2; the character itself is GR1's, with the per-format GR4–GR8 identities; SR13 admits
    ///     the operand as "a figurative constant that is permitted in a MOVE statement to a receiving item of
    ///     that category"). The fill character comes from the ONE figurative service, with the group's own
    ///     category so a national / bit group reads ITS anchor rather than the alphanumeric PCS.</item>
    ///   <item><c>ALL "literal"</c> — literal-1 repeated to the width (§8.3.3.6.4 GR2, the same rule).</item>
    ///   <item>a quoted literal — its characters (or, for a bit group, its BOOLEAN POSITIONS — <c>B"1010"</c> /
    ///     <c>BX"A"</c>, §8.3.3.4), decoded.</item>
    /// </list></summary>
    internal static GroupArea? AreaOf(DataItem group, EmitContext ctx, Subscripts subs = default)
    {
        // ⛔ BOTH VALUE CARRIERS, through the ONE reader (DataItem.ValueAt): a group entry may carry a FORMAT 2
        // (table) VALUE just as it may carry a format 1 — §13.18.63.3 SR16 pulls SR13 onto it and §13.18.63.4 GR5
        // still says the GROUP AREA is what gets initialized, per occurrence. Reading RawValue alone silently
        // discarded `05 GT OCCURS 2 VALUE "ABCD" FROM (1) TO (2).` at every edition (kb/Work PB505).
        if (group.ValueAt(subs) is not { } raw) return null;
        // ⛔ THE UNIT AND THE WIDTH, in one place. A GROUP-USAGE BIT group's area is its §13.18.29.4 GR1b
        // as-if PICTURE 1(m) — m BOOLEAN POSITIONS from the §8.5.1.6.3 walk; every other group's is its
        // ImageWidth CHARACTER positions.
        bool bits = group.GroupUsage is GroupUsage.Bit;
        // ⛔ A group that is NOT a bit group but carries a USAGE BIT descendant is measured by the bit walk
        // (DataItem.ImageWidth switches on HasBitDescendant, D19/PB43) while its AREA is a character run, so
        // neither unit describes it — and no such entry is conforming source: §13.18.63.3 SR14 requires every
        // data item subordinate to an ALPHANUMERIC group item carrying a VALUE to be explicitly or implicitly
        // usage DISPLAY, and a USAGE BIT leaf (or a nested GROUP-USAGE BIT group) is not
        // (DataBinder.CheckGroupValueDeclarations rejects it, COBOLNET1702); under a NATIONAL group
        // §13.18.29.3 SR3 requires usage national of every subordinate elementary item. Returning null keeps
        // BOTH lanes total over that non-conforming residue rather than slicing a width the members do not
        // tile — never a substitute for the diagnostic, which is stated where the rule is.
        if (!bits && group.HasBitDescendant) return null;
        int width = bits ? group.AsIfPic!.Length : group.ImageWidth;
        if (width <= 0) return null;
        // THE one §8.3.3.6.2 operand classifier decides which format this text is (kb/Work PB461): Formats 1-5
        // (ALL optional) fill the area, Format 6 (ALL literal-1) repeats literal-1 into it — both by §8.3.3.6.4
        // GR2, and both spellings the parse tree can produce.
        var op = FigurativeConstants.Classify(raw);
        string? text =
            op.Kind is { } kind
                ? new string(FigurativeConstants.FillChar(kind, ctx.Data.Collating,
                        group.AsIfPic?.Category ?? PicCategory.Alphanumeric, ctx.Data.NationalCollating), width)
            : op.AllLiteral is { } all ? EmitText.RepeatToWidth(CobolLiteral.Decode(all), width)
            : CobolLiteral.IsStringLiteral(raw) ? CobolLiteral.Decode(raw)
            : null;
        if (text is null) return null;
        // GR7 sends the literal through §14.6.8, whose arm is chosen by the RECEIVING item's category: a
        // category-boolean area is filled "into the corresponding boolean positions … with ZERO fill or
        // truncation to the right" (§14.6.8.6 — the boolean zero, not the space that would not be a boolean
        // position at all), every other category with space fill (§14.6.8.5). GR7's own exception applies to
        // both: initialization is not affected by JUSTIFIED and no editing takes place.
        char pad = bits ? '0' : ' ';
        return new GroupArea(text.Length >= width ? text[..width] : text.PadRight(width, pad), bits);
    }

    /// <summary>Whether the group's subtree can take the area text as a compile-time POSITIONAL slice per
    /// subordinate. Every admitted leaf is string-stored or a native CHARACTER-FORM numeric, so its slice IS its
    /// own image and the offsets are character offsets.
    ///
    /// <para>⚠ The usage arms below are NOT a Tier-C boundary and are no longer a known gap (they were read as
    /// one, and PB184 was registered against them). §13.18.63.3 SR14 makes a BINARY / PACKED / float / INDEX /
    /// BIT leaf under an ALPHANUMERIC group item carrying a VALUE non-conforming, and
    /// <c>DataBinder.CheckGroupValueDeclarations</c> now rejects it (COBOLNET1702) — so for that population the
    /// predicate is answering about source the binder has already screened, and it is kept as the defensive
    /// statement of what a positional character slice can mean. The <c>Class is null</c> arm is the live one: a
    /// shared-storage (REDEFINES-class) subtree is seeded through its BACKING by
    /// <see cref="GroupImageCodec.ImageInitOf"/> instead, from the same <see cref="AreaOf"/> rule.</para>
    /// <para>⛔ SR14 IS WRITTEN ABOUT AN ALPHANUMERIC GROUP AND DOES NOT SCREEN A NATIONAL ONE (kb/Work PB646) —
    /// a <c>GROUP-USAGE NATIONAL</c> group carrying a VALUE reaches here with every leaf at usage NATIONAL by
    /// §13.18.29.3 SR3, numeric leaves included by that rule's own wording, so the numeric arm below is LIVE
    /// source for it, not a defensive statement. Reading the character-form predicate rather than a usage
    /// spelling is what keeps the two screens from disagreeing.</para>
    /// <para>A BIT group passes on its <c>Boolean</c> arm and takes <see cref="SliceBitInit"/> rather than
    /// <see cref="SliceInit"/>: every member of a bit group is a bit leaf or a nested bit group
    /// (§13.18.29.3 SR2), so each one's slice IS its own boolean carrier — the same property in the other
    /// unit.</para></summary>
    private static bool DistributableSubtree(DataItem item) =>
        item.Class is null && (item.IsGroup
            ? item.Children.All(DistributableSubtree)
            : item.StoreAsImage || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean   // string-stored — the slice is the chars (D-N4 identity)
              // ⛔ BOTH CHARACTER-FORM USAGES (PicInfo.IsCharacterFormNumeric, kb/Work PB646): the slice is
              // CHARACTER positions and a national-form numeric leaf's image is the same digit run its DISPLAY
              // twin holds (design D-N7), so it decodes from its slice exactly as that twin does. This arm read
              // DISPLAY alone, and a NATIONAL GROUP is the case §13.18.63.3 SR14 does NOT screen — SR14's usage
              // requirement is written about items "subordinate to an alphanumeric group item", while
              // §13.18.29.3 SR3 requires "All elementary items subordinate to the subject of the entry shall be
              // explicitly or implicitly described as usage national" and contemplates numeric ones by name
              // ("Any signed numeric data items shall be described with the SIGN IS SEPARATE clause"). So a
              // conforming `01 NG GROUP-USAGE NATIONAL VALUE N"AB123". 05 NGA PIC N(2). 05 NGB PIC 9(3).`
              // reached this predicate, failed on NGB, and took the member-wise default for the WHOLE subtree:
              // MEASURED `NGA=[  ] NGB=[000]` against its alphanumeric twin's `AGA=[AB] AGB=[123]`. Before
              // PB646 the shape could not compile at all, so the DISPLAY-only arm had never been contradicted.
              || item.Pic is { IsCharacterFormNumeric: true });

    /// <summary>Build the composed initializer of <paramref name="item"/> from its positional <paramref name="slice"/>
    /// of the group VALUE text — each subordinate (and each OCCURS occurrence) takes its own window.</summary>
    private static string SliceInit(DataItem item, string slice)
    {
        // A native (long-stored) CHARACTER-FORM numeric leaf decodes its zoned slice to the unscaled value
        // (sign-aware overpunch/separate decode — the same ParseDisplay every image read uses). Both usages
        // §13.18.60.4 GR7/GR8 name, through the one PicInfo.IsCharacterFormNumeric predicate: the national
        // form's digit characters ARE the twin's (design D-N7), so one decode serves both and the arm cannot
        // drift from DistributableSubtree's admission above. String-stored leaves (alphanumeric / edited /
        // national / boolean / StoreAsImage) keep the characters.
        if (!item.IsGroup && !item.StoreAsImage && item.Pic is { IsCharacterFormNumeric: true })
            return $"({item.ElementType}){RuntimeApi.NumParseDisplay(EmitText.CsLiteral(slice), item.ProfileName)}";
        if (!item.IsGroup) return EmitText.CsLiteral(slice);
        var parts = new List<string>();
        int off = 0;
        foreach (var c in item.Children)
        {
            int w = c.ImageWidth;
            if (c.Occurs is { } n)
            {
                var elems = new List<string>();
                for (int k = 0; k < n; k++) elems.Add(SliceInit(c, slice.Substring(off + k * w, w)));
                parts.Add($"{c.CsName} = new {c.ElementType}[] {{ {string.Join(", ", elems)} }}");
                off += w * n;
            }
            else
            {
                parts.Add($"{c.CsName} = {SliceInit(c, slice.Substring(off, w))}");
                off += w;
            }
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>Build the composed initializer of a BIT group from its §13.18.63.4 GR5 area — the group's m
    /// boolean positions — by giving each member the positions §8.5.1.6.3 places it at. The bit twin of
    /// <see cref="SliceInit"/>, and it is a separate walk for exactly one reason: the offsets are BIT offsets
    /// and they are not a running sum of the members' widths.
    ///
    /// <para>⛔ The placement comes from <see cref="BitLayout.StartBitWithin"/>, THE one §8.5.1.6.3 walk. A
    /// private cursor here would be another copy of the placement law and would silently disagree with it
    /// wherever two members are NOT adjacent — a member whose LEVEL NUMBER differs from its predecessor's is
    /// not "immediately following … an item of the same level", so it starts at "the first bit position of the
    /// first available byte" and the bits between are implicit filler, which a sum cannot express (this is the
    /// same reason <see cref="BitLayout.ExtentBits"/> is a cursor walk and not a sum).</para>
    ///
    /// <para>A redefining member overlays its target and occupies no positions of its own (§13.18.44), so it
    /// takes no slice; <see cref="DistributableSubtree"/> has already excluded any subtree that has one, and
    /// skipping it here states the rule rather than relying on that.</para></summary>
    private static string SliceBitInit(DataItem item, string area)
    {
        if (!item.IsGroup) return EmitText.CsLiteral(area);
        var parts = new List<string>();
        foreach (var c in item.Children)
        {
            if (c.RedefinesTargetName is not null) continue;
            int at = BitLayout.StartBitWithin(item, c);
            if (at < 0) continue;      // an unmodelled overlay chain — StartBitWithin's documented -1
            int per = BitLayout.WidthBits(c);
            if (c.Occurs is { } n)
            {
                // ISO §13.18.63.4 GR9 gives EVERY occurrence the value at its own position; the stride is the
                // per-occurrence BIT extent, which is what makes a `PIC 1(4) OCCURS 6` table six sub-byte
                // windows rather than six bytes. ⛔ The stride and the WINDOW WIDTH were one local until
                // kb/Work PB487; §13.18.1.4 GR2 splits them, because an ALIGNED occurrence still occupies only
                // its declared bits but the NEXT one starts at the following byte.
                int stride = BitLayout.StrideBits(c);
                var elems = new List<string>();
                for (int k = 0; k < n; k++) elems.Add(SliceBitInit(c, BitWindow(area, at + k * stride, per)));
                parts.Add($"{c.CsName} = new {c.ElementType}[] {{ {string.Join(", ", elems)} }}");
            }
            else parts.Add($"{c.CsName} = {SliceBitInit(c, BitWindow(area, at, per))}");
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>One member's window of the area's boolean positions, ZERO-filled past the area's end — the
    /// compile-time twin of <c>CobolBits.Slice</c>, and the same §14.6.8.6 boolean-zero pad
    /// <see cref="AreaOf"/> uses. A window can run past the area only when the group's own extent exceeds the
    /// positions the walk assigns (implicit filler, §8.5.1.6.3), so the fill is filler bits, which
    /// §13.18.63's all-zero boolean initial state makes zeros.</summary>
    private static string BitWindow(string area, int at, int count) =>
        at >= area.Length ? new string('0', count)
        : area.Substring(at, Math.Min(count, area.Length - at)).PadRight(count, '0');
}
