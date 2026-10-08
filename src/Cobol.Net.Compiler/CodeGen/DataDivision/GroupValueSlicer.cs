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
/// exclusion (kb/Work PB207); one producer, one answer.</para>
/// <para>⛔ <b>The area is never materialized at its full width</b> (kb/Work PB1722): it is
/// <paramref name="Head"/> (the literal's own positions) followed by <paramref name="Unit"/> repeated to
/// <paramref name="Width"/> — the figurative's one character, the ALL literal, or the GR7 pad — so a group VALUE
/// over a table of a hundred million positions is a few characters at compile time, its windows are compared by
/// phase (<see cref="SameWindow"/>), and the image lane repeats the unit at run time (<see cref="CsExpression"/>).
/// <paramref name="Unit"/> is reduced to its minimal period, so two windows of the repeated part are equal exactly
/// when their phases are.</para></summary>
internal readonly record struct GroupArea(string Head, string Unit, int Width, bool Bits)
{
    /// <summary>The area whose positions are <paramref name="head"/> (truncated to <paramref name="width"/>)
    /// followed by <paramref name="unit"/> repeated.</summary>
    public static GroupArea Of(string head, string unit, int width, bool bits) =>
        new(head.Length > width ? head[..width] : head, MinimalPeriod(unit), width, bits);

    /// <summary>The position at <paramref name="i"/> (0-based).</summary>
    public char At(int i) => i < Head.Length ? Head[i] : Unit[(i - Head.Length) % Unit.Length];

    /// <summary>The <paramref name="count"/> positions from <paramref name="at"/> as an area of their own;
    /// positions past <see cref="Width"/> read <paramref name="pad"/> (a bit member's implicit filler, §8.5.1.6.3).</summary>
    public GroupArea Window(int at, int count, char pad)
    {
        if (at + count > Width)
        {
            var chars = new char[count];
            for (int i = 0; i < count; i++) chars[i] = at + i < Width ? At(at + i) : pad;
            return Of(new string(chars), pad.ToString(), count, Bits);
        }
        if (at < Head.Length) return Of(Head.Substring(at, Math.Min(Head.Length - at, count)), Unit, count, Bits);
        int phase = (at - Head.Length) % Unit.Length;
        return Of("", Unit[phase..] + Unit[..phase], count, Bits);
    }

    /// <summary>Whether the windows of <paramref name="count"/> positions at <paramref name="a"/> and
    /// <paramref name="b"/> hold the same positions. Two windows wholly inside the repeated part are equal exactly
    /// when their phases agree (or, shorter than the unit, when their characters do); anything else is compared
    /// position by position, which only a window reaching into <see cref="Head"/> needs.</summary>
    public bool SameWindow(int a, int b, int count)
    {
        char pad = Bits ? '0' : ' ';
        if (a + count > Width || b + count > Width) return Window(a, count, pad).Text == Window(b, count, pad).Text;
        if (a >= Head.Length && b >= Head.Length && count >= Unit.Length)
            return (a - Head.Length) % Unit.Length == (b - Head.Length) % Unit.Length;
        for (int i = 0; i < count; i++)
            if (At(a + i) != At(b + i)) return false;
        return true;
    }

    /// <summary>How many occurrences of a <paramref name="stride"/>-position element it takes the repeated part to
    /// come back to the same phase — the period a table's windows repeat with there.</summary>
    public int PeriodFor(int stride) => Unit.Length / (int)Binding.Model.TableValuePlan.Gcd(Unit.Length, stride);

    /// <summary>The positions, materialized — for a LEAF's window, whose width its PICTURE bounds.</summary>
    public string Text
    {
        get
        {
            if (Width <= Head.Length) return Head[..Width];
            var sb = new System.Text.StringBuilder(Head, Width);
            while (sb.Length < Width) sb.Append(Unit, 0, Math.Min(Unit.Length, Width - sb.Length));
            return sb.ToString();
        }
    }

    /// <summary>The area as a C# string expression: the head, then the unit repeated at run time
    /// (<c>CobolString.Repeat</c>), then the unit's partial tail.</summary>
    public string CsExpression()
    {
        int rest = Width - Head.Length, reps = rest / Unit.Length, part = rest % Unit.Length;
        var parts = new List<string>();
        if (Head.Length > 0) parts.Add(EmitText.CsLiteral(Head));
        if (reps > 0) parts.Add(reps == 1 ? EmitText.CsLiteral(Unit) : RuntimeApi.StrRepeat(EmitText.CsLiteral(Unit), $"{reps}"));
        if (part > 0) parts.Add(EmitText.CsLiteral(Unit[..part]));
        return parts.Count switch { 0 => "\"\"", 1 => parts[0], _ => "(" + string.Join(" + ", parts) + ")" };
    }

    private static string MinimalPeriod(string unit)
    {
        for (int p = 1; p < unit.Length; p++)
        {
            if (unit.Length % p != 0) continue;
            bool periodic = true;
            for (int i = p; i < unit.Length && periodic; i++) periodic = unit[i] == unit[i - p];
            if (periodic) return unit[..p];
        }
        return unit;
    }
}

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
            return area.Bits ? SliceBitInit(group, area, subs.Count) : SliceInit(group, area, subs.Count);
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
        // GR7 sends the literal through §14.6.8, whose arm is chosen by the RECEIVING item's category: a
        // category-boolean area is filled "into the corresponding boolean positions … with ZERO fill or
        // truncation to the right" (§14.6.8.6 — the boolean zero, not the space that would not be a boolean
        // position at all), every other category with space fill (§14.6.8.5). GR7's own exception applies to
        // both: initialization is not affected by JUSTIFIED and no editing takes place. The repeat is the area's
        // UNIT, never a full-width string (kb/Work PB1722).
        string pad = bits ? "0" : " ";
        var op = FigurativeConstants.Classify(raw);
        if (op.Kind is { } kind)
            return GroupArea.Of("", FigurativeConstants.FillChar(kind, ctx.Data.Collating,
                group.AsIfPic?.Category ?? PicCategory.Alphanumeric, ctx.Data.NationalCollating).ToString(), width, bits);
        if (op.AllLiteral is { } all && CobolLiteral.Decode(all) is { Length: > 0 } unit)
            return GroupArea.Of("", unit, width, bits);
        return CobolLiteral.IsStringLiteral(raw) ? GroupArea.Of(CobolLiteral.Decode(raw), pad, width, bits) : null;
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
    private static string SliceInit(DataItem item, GroupArea slice, int depth)
    {
        // A native (long-stored) CHARACTER-FORM numeric leaf decodes its zoned slice to the unscaled value
        // (sign-aware overpunch/separate decode — the same ParseDisplay every image read uses). Both usages
        // §13.18.60.4 GR7/GR8 name, through the one PicInfo.IsCharacterFormNumeric predicate: the national
        // form's digit characters ARE the twin's (design D-N7), so one decode serves both and the arm cannot
        // drift from DistributableSubtree's admission above. String-stored leaves (alphanumeric / edited /
        // national / boolean / StoreAsImage) keep the characters.
        if (!item.IsGroup && !item.StoreAsImage && item.Pic is { IsCharacterFormNumeric: true })
            return $"({item.ElementType}){RuntimeApi.NumParseDisplay(EmitText.CsLiteral(slice.Text), item.ProfileName)}";
        if (!item.IsGroup) return EmitText.CsLiteral(slice.Text);
        var parts = new List<string>();
        int off = 0;
        foreach (var c in item.Children)
        {
            int w = c.ImageWidth;
            if (c.Occurs is { } n)
            {
                // One element composed per run of windows that repeat (kb/Work PB1722): a figurative fill makes the
                // whole table one run and an ALL literal a periodic one, so the text no longer grows with the
                // element count.
                int at = off;
                var runs = OccurrenceRunEmit.RunsOfRepeats(n, slice.PeriodFor(w),
                    (a, b) => slice.SameWindow(at + (a - 1) * w, at + (b - 1) * w, w));
                parts.Add($"{c.CsName} = " + OccurrenceRunEmit.Array(c.ElementType, n, runs,
                    o => SliceInit(c, slice.Window(at + (o - 1) * w, w, ' '), depth + 1), depth));
                off += w * n;
            }
            else
            {
                parts.Add($"{c.CsName} = {SliceInit(c, slice.Window(off, w, ' '), depth)}");
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
    private static string SliceBitInit(DataItem item, GroupArea area, int depth)
    {
        if (!item.IsGroup) return EmitText.CsLiteral(area.Text);
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
                var runs = OccurrenceRunEmit.RunsOfRepeats(n, area.PeriodFor(stride),   // kb/Work PB1722
                    (a, b) => area.SameWindow(at + (a - 1) * stride, at + (b - 1) * stride, per));
                parts.Add($"{c.CsName} = " + OccurrenceRunEmit.Array(c.ElementType, n, runs,
                    o => SliceBitInit(c, BitWindow(area, at + (o - 1) * stride, per), depth + 1), depth));
            }
            else parts.Add($"{c.CsName} = {SliceBitInit(c, BitWindow(area, at, per), depth)}");
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>One member's window of the area's boolean positions, ZERO-filled past the area's end — the
    /// compile-time twin of <c>CobolBits.Slice</c>, and the same §14.6.8.6 boolean-zero pad
    /// <see cref="AreaOf"/> uses. A window can run past the area only when the group's own extent exceeds the
    /// positions the walk assigns (implicit filler, §8.5.1.6.3), so the fill is filler bits, which
    /// §13.18.63's all-zero boolean initial state makes zeros.</summary>
    private static GroupArea BitWindow(GroupArea area, int at, int count) => area.Window(at, count, '0');
}
