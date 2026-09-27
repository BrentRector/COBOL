// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The whole-group character-image codec (P7 Step 9l; COBOLNET_DESIGN §14.4): the generated
/// <c>AsImage</c>/<c>FromImage</c> pair every image-capable group carries — string leaves verbatim, NATIVE
/// fixed-point leaves through their zoned image profile — plus the compile-time INITIAL-image composer that
/// seeds Tier-B REDEFINES backings. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupImageCodec(EmitContext ctx, PhysicalModel phys, ValueInitializer vals)
{
    /// <summary>The C# string-expression for an item's INITIAL character image (used to seed a Tier-B backing from
    /// the canonical's VALUE): a group concatenates its leaves' images; an elementary item formats its VALUE (numeric
    /// → <c>CobolNum.FormatDisplay</c>; alphanumeric/edited → the stored string; figurative/default per width). A
    /// fixed-OCCURS entry's image repeats <c>Occurs</c> times — every occurrence takes the VALUE (ISO §13.18.63 GR9;
    /// the recursion runs through this wrapper, so nested OCCURS repeat too).
    /// <para><paramref name="useValues"/> false suppresses every VALUE and composes the CATEGORY-DEFAULT initial
    /// image instead — the §13.18.63 GR4a shape, where "a PLAIN external item's VALUE takes effect only during
    /// INITIALIZE", so its run-unit cell must seed blank/zero rather than VALUE-composed. It is the SAME
    /// composition either way, which is the point: a byte-form leaf's default is the ZERO ENCODING of its pinned
    /// form (radix-2 / BCD / IEEE / INDEX bytes), never a run of ASCII '0' characters. The EXTERNAL path used to
    /// hand-roll that default in the BINDER (<c>CallInitialImage</c>, a second seeder with a char-fill model that
    /// predated the byte forms); one seeder, one flag (kb/Work PB164 — one-mechanism-per-job).</para></summary>
    /// <param name="subs">The OCCURRENCE CONTEXT — the subscripts of every OCCURS level entered on the way down
    /// from the record root, most inclusive first (the twin of <see cref="ValueInitializer.FieldInit"/>'s, so the
    /// two lanes key a Format 2 (table) VALUE the same way).</param>
    public string ImageInitOf(DataItem item, bool useValues = true, Subscripts subs = default,
        SeedRecipe recipe = SeedRecipe.InitialState)
    {
        // ⛔ A FORMAT 2 (table) VALUE GIVES EACH OCCURRENCE ITS OWN IMAGE — §13.18.63.4 GR12 ("A format 2 VALUE
        // clause initializes a table element to the value of literal-1"), GR13 (cyclic reuse under TO), GR14
        // (no TO = fill to the maximum), GR15 (a later FROM wins on overlap). This method read only
        // item.RawValue, which is null for a table VALUE (the two carriers are mutually exclusive on DataItem),
        // and the StrRepeat below then seeded EVERY occurrence with the same VALUE-less default: the table
        // VALUE was silently DISCARDED for every image-stored leaf — a Tier-B REDEFINES member, an EXTERNAL
        // cell, a BASED record, an OO backing. Measured on 3324d794 with
        // `05 B PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).` inside a REDEFINES-aliased group: the group
        // image came back `65 65 00 00 00 00 67 67`, the seed simply absent (kb/Work PB208).
        // The per-occurrence walk is now the SAME shape the record-struct lane uses — one composition per
        // occurrence against its own subscript tuple — so it covers a SUBORDINATE-item table VALUE and a
        // multi-dimension odometer without a second rule being written here (kb/Work PB505). An occurrence
        // outside every FROM..TO range takes the same VALUE-less image it always took.
        if (useValues && item.ContainsTableValue && item.Occurs is { } occ and > 0)
            return "(" + string.Join(" + ", Enumerable.Range(1, occ)
                .Select(o => ImageInitOfOne(item, useValues, subs.With(o), recipe))) + ")";
        string one = ImageInitOfOne(item, useValues, item.Occurs is > 0 ? subs.With(1) : subs, recipe);
        return item.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
    }
    /// <param name="subs">The subscript tuple of THIS occurrence — the twin of
    /// <see cref="ValueInitializer.InitializerFor"/>'s parameter of the same name, so the image lane and the
    /// native-field lane compose one occurrence from the same literal text (kb/Work PB208/PB505).</param>
    private string ImageInitOfOne(DataItem item, bool useValues, Subscripts subs, SeedRecipe recipe)
    {
        // ⛔ A POINTER-CLASS LEAF CONTRIBUTES RESERVED BYTES AND NO IMAGE (kb/Work PB231 — the pointer third).
        // Its value is a managed reference held in the area's MANAGED SLOT (Place.SlotWindow / StorageCell.SlotAt),
        // whose unwritten state IS null — §14.9.3.4 GR9's "data items of class object or class pointer in the
        // allocated storage are initialized to null" and §13.18.63.4's "data items of class message-tag, class
        // object, and class pointer are initialized to null" both hold with no seeding here. What the BYTE image
        // owes the shared area is exactly the item's storage extent of placeholder positions, so §14.9.3.4 GR3's
        // byte quantity and every FOLLOWING member's class offset are what a byte-addressed area says they are —
        // without this the pointer contributed its PICTURE-less zero-width nothing and displaced the rest.
        // Placed beside the national coding below because it is the same kind of decision: how the member's
        // value carrier relates to the bytes it occupies.
        if (SlotWindow.CarriedBySlot(item)) return SlotPlaceholder(item.ByteWidth);
        // ⛔ A DYNAMIC-LENGTH LEAF CONTRIBUTES NO BYTES AT ALL (kb/Work PB1026): its content rides the cell's
        // dynamic slot (Place.DynSlotWindow), and it occupies zero positions of the area's fixed run — its
        // ByteWidth, §8.5.1.12.3's "all dynamic-length elementary items are considered to be of zero length". Its
        // VALUE, if any, is the slot's seed (CellDynSeeds). A one-character fill here displaced every following
        // member of an ADDRESS-OF-taken variable-length group by one position.
        if (DynSlotWindow.CarriedBySlot(item)) return "\"\"";
        string image = CarrierInitOfOne(item, useValues, subs, recipe);
        // ⛔ A NATIONAL LEAF'S SEED IS ITS BYTES, NOT ITS CARRIER (kb/Work PB231). Everything this method seeds
        // is a BYTE-ADDRESSED shared area — a Tier-B REDEFINES backing, an EXTERNAL run-unit cell, a
        // BASED/ADDRESS-OF cell, an OO backing — and a national character position occupies TWO of those bytes
        // (§13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two, UTF-16BE). Applied HERE, once,
        // over whatever the carrier arms below composed, so the VALUE arm, the OPTIONS INITIALIZE fill arm and
        // the no-clause space arm cannot each get it separately right or separately wrong — the same
        // one-transform discipline the USAGE BIT arms use with CobolBits.Pack. MEASURED before the fix on
        // `01 K. 05 KT PIC N(2) OCCURS 3. 01 KX REDEFINES K PIC X(12).`: KT(1)'s initial state read back as
        // U+2020 U+2020 (two 0x20 fill BYTES paired) instead of the national spaces its struct-stored twin
        // gets — a silent wrong answer on the first read of an uninitialized national member.
        // A GROUP is never wrapped: PositionsOf answers null for it, and its national children were each
        // wrapped by their own recursion through here.
        return NationalWindow.PositionsOf(item) is not null ? RuntimeApi.NatBytes(image) : image;
    }

    /// <summary>⛔ THE ONE SEEDER OF A CELL'S DYNAMIC-LENGTH HALF (kb/Work PB1026) — the twin of
    /// <see cref="ImageInitOf"/> for the slots <see cref="ImageInitOfOne"/> leaves out: a
    /// <c>.SeedDyn(ordinal, content)</c> call per dynamic-length item of <paramref name="root"/>'s cell-backed class
    /// whose initial content is not empty — §8.6.4 via the VALUE clause's own recipe
    /// (<see cref="ValueInitializer.InitializerFrom"/>'s dynamic-length arm: MOVE-like, truncated to the maximum
    /// size; absent a VALUE the initial length is zero, which an unwritten slot already is). Empty when there is
    /// nothing to seed, so every cell without such an item is emitted exactly as before.</summary>
    public string CellDynSeeds(DataItem root, bool useValues = true)
    {
        if (!useValues) return "";
        var seeds = DataItem.DescendantsOf(root).Prepend(root)
            .Where(d => DynSlotWindow.CarriedBySlot(d) && d.ClassDynOrdinal >= 0 && d.RawValue is not null)
            .Select(d => $".{nameof(CobolNet.Runtime.StorageCell.SeedDyn)}({d.ClassDynOrdinal}, {vals.InitializerFrom(d, d.RawValue)})");
        return string.Concat(seeds);
    }

    /// <summary>One MEMBER's contribution to its group's compile-time image seed — the COMPILE-TIME twin of
    /// <see cref="AsImageOf"/>, arm for arm, so the seed a Tier-B backing starts at and the image
    /// <c>AsImage()</c> composes at run time are the same bytes (kb/Work PB584).
    /// <para>A §8.5.1.6.3 bit RUN images as its PACKED bits: the run's LEADER packs every member's INITIAL
    /// carrier concatenated (the compile-time twin of <c>BitCarrierOf</c>) and a CONTINUATION contributes
    /// nothing — which is what makes a MIXED bit/character group's seed exactly as wide as the group instead of
    /// one byte per bit member. Everything else recurses through <see cref="ImageInitOf"/>, so the national
    /// coding and the pointer-slot reservation ride <see cref="ImageInitOfOne"/> as they do for every caller.</para>
    /// <para>⚠ The walk is over <c>item.Children</c> rather than <c>PhysicalModel.PhysicalChildrenOf</c>, and the
    /// reason is measured: <c>DataBinder.AssignClassOffsets</c> marks every DESCENDANT of a redefines class
    /// <c>IsCanonical = false</c>, so the physical walk answers EMPTY for the children of a Tier-B canonical —
    /// the exact group this seed exists for. What the two composers must share is the RUN LAW, and they now do
    /// (<see cref="BitLayout.RunsOf"/>).</para></summary>
    private string InitImageOfMember(DataItem c, BitRunMap runs, bool useValues, Subscripts subs, SeedRecipe recipe) =>
        runs.RunLedBy(c) is { } run
            ? RuntimeApi.BitsPack(string.Join(" + ", run.Select(m => InitialBitCarrierOf(m, useValues, subs))),
                                  $"{run.Sum(BitLayout.RunBits)}")
        : runs.IsInRun(c) ? "\"\""
        : ImageInitOf(c, useValues, subs, recipe);

    /// <summary>The item's initial image in its VALUE CARRIER's own units — see <see cref="ImageInitOfOne"/>,
    /// which applies the storage coding on top (kb/Work PB231).</summary>
    private string CarrierInitOfOne(DataItem item, bool useValues, Subscripts subs, SeedRecipe recipe)
    {
        if (item.IsGroup)
        {
            // ⛔ A GROUP-LEVEL VALUE INITIALIZES THE AREA (ISO §13.18.63.4 GR5 — "the group area is initialized
            // without consideration for the individual elementary or group items contained within this group"),
            // so it REPLACES the member-wise composition below rather than being composed alongside it. The one
            // area rule is GroupValueSlicer.AreaTextOf, shared with the record-struct lane.
            //
            // ⚠ THIS ARM WAS MISSING, and it was a live silent wrong answer on LEGAL source (kb/Work PB184's
            // sibling sweep). ImageInitOf is THE seeder for every character-image backing since the PB164
            // consolidation — Tier-B REDEFINES backings (RecordStructEmitter / PhysicalModel), EXTERNAL run-unit
            // cells (DataEmitter.ExternalCellSeed), BASED/ADDRESS-OF cells (ProgramEmitter / PtrEmitter) and the
            // OO backings — and it walked straight past the group's own RawValue into the members. Measured on
            // 8ca74a3d: `01 G VALUE "ABCD". 05 A PIC X(2). 05 B PIC X(2). 01 R REDEFINES G PIC X(4).` left A, B
            // and R all SPACES, while the identical group WITHOUT the REDEFINES initialized "AB"/"CD" — the
            // whole VALUE lost to nothing but the presence of an alias. §13.18.63.3 SR12 bars a VALUE in the
            // redefinING entry, never in the redefined one, so that program is conforming.
            // (The seventh instance of the two-arm-dispatch shape: the slicer's arm was written, the codec's
            // was not, and only the slicer's lane was ever tested.)
            //
            // ⛔ The UNIT is not restated here. It used to be an exclusion keyed on GROUP-USAGE BIT — a
            // predicate NARROWER than the fact it protects — and then, once the exclusion moved into the area
            // rule, a refusal. Neither is here now: GroupValueSlicer.AreaOf answers with the area AND its unit,
            // so this lane cannot disagree with the record-struct lane about what a bit group's area is
            // (kb/Work PB207).
            if (useValues && recipe is SeedRecipe.InitialState && GroupValueSlicer.AreaOf(item, ctx, subs) is { } area)
                // A bit group's area is m BOOLEAN POSITIONS (§13.18.29.4 GR1b); its IMAGE is those positions
                // PACKED — ceil(m/8) characters through CobolBits.Pack, the ONE bit-order law (D19/PB43), never
                // a second packer written here. The count is asked of ExtentBits, not taken from the area's own
                // Length: the two are equal by AreaOf's contract, and asking the LAYOUT keeps the image the
                // group's full width even if a future area were handed back short (Pack zero-fills the rest).
                return area.Bits
                    ? RuntimeApi.BitsPack(EmitText.CsLiteral(area.Text), $"{BitLayout.ExtentBits(item)}")
                    : EmitText.CsLiteral(area.Text);

            // ⛔ A BIT GROUP'S IMAGE IS ITS PACKED AREA, NOT ITS MEMBERS' IMAGES CONCATENATED. Each bit member
            // images as its own ceil(n/8) characters, and §8.5.1.6.3 makes same-level bit members SHARE bytes —
            // so the concatenation is wider than the group. MEASURED at 62a36759 on
            // `01 K GROUP-USAGE BIT. 05 K1 PIC 1(2) VALUE B"11". 05 K2 PIC 1(2) VALUE B"01".
            //  01 KV REDEFINES K PIC X(1).`: K2 read back as `00` and the backing byte was 0xC0, because the
            // 2-character composition was truncated to the group's 1-character width and K2's byte fell off.
            // The composition is BitAreaOf's — the same §8.5.1.6.3 placement the VALUE arm above and
            // AsBits()/FromBits use.
            if (item.GroupUsage is GroupUsage.Bit)
                return RuntimeApi.BitsPack(BitAreaOf(item, m => InitialBitCarrierOf(m, useValues, subs)),
                                           $"{BitLayout.ExtentBits(item)}");

            // ⛔ THE SEED COMPOSES OVER THE PHYSICAL FIELD LIST, NOT OVER item.Children (kb/Work PB584). A MIXED
            // bit/character group is the case that proves it: §8.5.1.6.3 puts "an elementary bit data item
            // immediately following an elementary bit data item or bit group item of the same level" at the next
            // BIT position, so consecutive same-level bit members SHARE a byte, and only PhysicalModel computes
            // those runs. Walking the children instead gave each bit member its OWN ceil(n/8) characters, so the
            // composition was WIDER than the group and every member after the first run shifted. MEASURED at
            // 2f6b38c61 on `01 CTL. 05 H1 PIC 1(4) USAGE BIT VALUE B"0100". 05 H2 PIC 1(4) USAGE BIT VALUE
            // B"0001". 05 H3 PIC X(1) VALUE "B". 01 CV REDEFINES CTL PIC X(2).`: the members read back
            // [0100][0000][DLE] and CV's bytes were 0x40 0x10, where §8.5.1.6.3's one run gives 0x41 ('A') and
            // §13.18.44.4 GR1's storage association then puts "B" in CV(2:1). The identical group WITHOUT the
            // alias — the record-struct lane, which already walks PhysicalChildrenOf — gave [0100][0001][B].
            // Two composers for one image; both now ask the ONE run law, so a run cannot be resolved two ways.
            // ⚠ What they share is the LAW and not the field list: `PhysicalModel.PhysicalChildrenOf` answers
            // EMPTY for the children of a Tier-B canonical — `DataBinder.AssignClassOffsets` marks every
            // DESCENDANT of a redefines class `IsCanonical = false` — and a Tier-B group is the one this seed
            // exists for. So the walk stays over `item.Children`, and a REDEFINING child contributes nothing
            // because its target already composed that storage (the filter below, as before).
            // The run map is computed over ALL the children, redefining ones included, exactly as
            // `PhysicalModel` computes it — `BitLayout.RunsOf` excludes them itself, so the two agree by
            // construction rather than by both remembering to filter first.
            var runs = BitLayout.RunsOf(item.Children);
            var parts = item.Children.Where(c => (c.IsGroup || c.IsElementary) && c.RedefinesTargetName is null)
                .Select(c => InitImageOfMember(c, runs, useValues, subs, recipe));
            return item.Children.Count > 0 ? "(" + string.Join(" + ", parts) + ")" : "\"\"";
        }
        var pic = item.Pic!;
        // ⛔ THE ONE READER, shared with ValueInitializer.InitializerFor: the Format-1 VALUE or this occurrence's
        // Format-2 literal (DataItem.ValueAt) — one recipe, both lanes.
        string? effRaw = item.ValueAt(subs);
        if (useValues && effRaw is { } raw)
        {
            // ⛔ THE BOOLEAN ARM IS FIRST, AND THE ORDER IS THE FIX (kb/Work PB584). It used to sit BELOW the
            // generic figurative arm, so `PIC 1(4) USAGE BIT VALUE ZERO` seeded FOUR carrier characters where
            // the packed image is ONE — the figurative return knows the item's boolean POSITIONS and nothing
            // about its storage. MEASURED at 2f6b38c61 on `01 B PIC 1(4) USAGE BIT VALUE ZERO. 01 BV REDEFINES
            // B PIC X(1).`: the byte came back 0x30 (the character '0') where §13.18.60.4 GR5's packed image of
            // four zero bits is 0x00, and B itself then read [0011]. One arm, one reader: the CARRIER is
            // ValueInitializer.BooleanCarrierOf's answer for every lane, and the USAGE decides only whether
            // those positions are packed.
            if (pic.Category is PicCategory.Boolean) return BooleanImageOf(vals.BooleanCarrierOf(raw, pic), pic);
            if (vals.FigurativeInitializer(raw, pic) is { } fig && pic.Category is not PicCategory.Numeric) return fig;
            // A NUMERIC member contributes the BYTES of its VALUE through its own pinned byte form — zoned
            // digits for DISPLAY, radix-2 / BCD for BINARY / PACKED (V59). ⛔ ONE ENCODER, so the width is the
            // item's StorageWidth BY CONSTRUCTION (kb/Work PB188): the CCVS-leniency spelling that used to sit
            // here stored `pic.Length` CHARACTERS — the PICTURE's digit count — which for a byte-form member is
            // simply a different number (`PIC 9(4) COMP` is 4 digits and 2 bytes) and displaced every following
            // member of the image. The leniency itself survives as what it always meant: an alphanumeric literal
            // on a numeric item is read AS the numeric literal §13.18.63.3 SR2 asked for (NC107A's
            // `PIC 999 VALUE "000"` under a REDEFINES), which is the SAME encode — hence one arm, not two.
            if (pic.Category is PicCategory.Numeric && !pic.IsFloat && vals.FigurativeInitializer(raw, pic) is null)
                return RuntimeApi.NumFormatImage(
                    EmitText.UnscaledAtScale(raw.StartsWith('"') ? CobolLiteral.Decode(raw) : raw, pic.Scale),
                    item.ProfileName);
            // A NUMERIC literal VALUE on a numeric-edited member contributes its EDITED image (§13.18.63 GR6) —
            // for a format-2 (LOCALE) member a RUNTIME image (no compile-time image exists, §13.18.40.5 r11;
            // the ONE producer, RuntimeApi.LocaleEditCompose — PB64 T6; the EditCompose arm's mask deref would NRE).
            if (pic.LocaleEdit is not null && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var luv, out int lsc))
                return RuntimeApi.LocaleEditCompose(pic, luv, lsc, item.BlankWhenZero);
            if (pic.Category is PicCategory.NumericEdited && !raw.StartsWith('"')
                && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
                return EmitText.CsLiteral(RuntimeApi.EditCompose(uv, sc, pic.EditMask!,
                    item.BlankWhenZero, pic.CurrencyString, ctx.Data.DecimalPointIsComma, pic.EditingRules));
            if (pic.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
            if (pic.Category is PicCategory.National)
                return RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}");
        }
        // A FLOAT member's image is its IEEE window bytes (the Step D arm-1 dissolution — the ' '×Length
        // fall-through seeded ZERO characters for the PICTURE-less float shapes, Length 0): the VALUE
        // literal through the ONE recipe, else the zero encoding.
        if (pic.Category is PicCategory.Numeric && pic.IsFloat)
            return RuntimeApi.NumFormatImageFloat(
                useValues && effRaw is { } fraw && !fraw.StartsWith('"')
                    && vals.FigurativeInitializer(fraw, pic) is null
                    ? ValueInitializer.RawValueAsFloat(fraw, pic) : "0d",
                item.ProfileName, pic.IsSingle);
        // ⛔ THE TIER-B IMAGE ARM'S NO-VALUE SEED — §14.6.2.3.2 action 1's BACKGROUND on the image axis
        // (kb/Work PB152). This was the arm PB151 could not reach: the ALLOCATE fix landed with its fill decoder
        // PRIVATE to PtrEmitter, so this fall-through went on hardcoding ' ' / '0' with no way to consult the
        // OPTIONS model. It now asks the SAME choke point the native-field arm asks (InitialStateBackground),
        // one call apart, and a drift test asserts the two agree for every category × usage. The baselines below
        // are unchanged and remain what a no-clause program gets.
        if (vals.NoValueSeed(item, pic, recipe) is { } bg) return bg;
        return pic.Category is PicCategory.Numeric && !pic.IsFloat
            ? RuntimeApi.NumFormatImage("0L", item.ProfileName)
            // Boolean initial state — zeros (§13.18.63). A USAGE BIT item's zeros are PACKED, so the seed is
            // ceil(n/8) zero BYTES rather than n zero characters (D19/PB43); the all-zero bit pattern makes the
            // packed form '\0' repeated, which is what the AsImage side would produce for the same value.
            : pic.Category is PicCategory.Boolean
                ? pic.Usage is Usage.Bit
                    ? $"new string('\\0', {BitLayout.Characters(pic.Length)})"
                    : $"new string('0', {pic.Length})"
                : $"new string(' ', {pic.Length})";
    }

    /// <summary>A boolean item's IMAGE from its boolean CARRIER — the one place the USAGE decides how the
    /// positions are stored (ISO §13.18.60.4 GR5: a usage-bit item's positions are PACKED, high-order first,
    /// into <c>ceil(n/8)</c> characters through <c>CobolBits.Pack</c>, D19/PB43; a usage-display boolean item's
    /// positions ARE its characters). Separating it from the carrier is what keeps the VALUE rules
    /// (<see cref="ValueInitializer.BooleanCarrierOf"/>) out of the storage question — the two were entangled,
    /// and a figurative VALUE therefore reached the image as carrier characters (kb/Work PB584).</summary>
    private static string BooleanImageOf(string carrier, PicInfo pic) =>
        pic.Usage is Usage.Bit ? RuntimeApi.BitsPack(carrier, $"{pic.Length}") : carrier;

    /// <summary>Emit a variable-length group's <c>CurrentImage()</c> — the §14.9.11.4 GR7 implementor-defined
    /// DISPLAY format, documented as CONFORMANCE.md A.1 item 57 (kb/Work PB164): the members' images in
    /// declaration order, each FIXED member contributing exactly its <see cref="AsImageOf"/> recipe (the ONE
    /// member-image law — no second copy), each dynamic-length leaf its CURRENT content, each dynamic-capacity
    /// table every occurrence at its CURRENT capacity, and each nested SCALAR variable-length group its own
    /// <c>CurrentImage()</c>. The geometry follows the §15.50.4 r7 LENGTH sum in CHARACTER POSITIONS —
    /// <c>FUNCTION LENGTH(G)</c> equals <c>CurrentImage().Length</c> except that a NATIONAL member displays
    /// one character per position while LENGTH counts its two bytes (the sanctioned D-N1/D-N3 divergence;
    /// the golden pins the relationship with a national member so it is observed, not assumed). DISPLAY-only:
    /// the static record codec (<c>AsImage</c>/<c>FromImage</c>) still excludes these groups (D9 — no fixed
    /// record window).</summary>
    public void EmitCurrentImageMethod(DataItem group, CodeWriter w)
    {
        var dyn = group.Children
            .Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)
                && (c.IsDynamicTable || c.IsDynamicLength || (c.IsGroup && !c.IsImageCapable)))
            .ToDictionary(c => c.CsName, StringComparer.Ordinal);
        var parts = phys.PhysicalChildrenOf(group)
            .Select(f => dyn.TryGetValue(f.Name, out var d) ? CurrentMemberImage(d) : AsImageOf(f))
            .ToList();
        w.Line($"public readonly string CurrentImage() => {(parts.Count > 0 ? string.Join(" + ", parts) : "\"\"")};");
    }

    private string CurrentMemberImage(DataItem d) =>
        d.IsDynamicTable
            ? d.IsGroup ? $"{d.CsName}.CurrentImage(static __e => __e.AsImage())"
            // The element lane mirrors PhysicalModel's numLeaf rule exactly: a NATIVE numeric element goes
            // through its byte-form lane (float via the distinctly-named IEEE lane), a string-carried element
            // (alphanumeric / edited / StoreAsImage) passes through.
            : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
                ? (d.Pic.IsFloat
                    ? $"{d.CsName}.CurrentImage(__e => {RuntimeApi.NumFormatImageFloat("__e", d.ProfileName, d.Pic.IsSingle)})"
                    : $"{d.CsName}.CurrentImage(__e => {RuntimeApi.NumFormatImage("__e", d.ProfileName)})")
                : $"{d.CsName}.CurrentImage(static __e => __e)"
        : d.IsDynamicLength ? d.CsName   // §15.50.4 r7b — the current content at its current length
        : $"{d.CsName}.CurrentImage()";  // a nested variable-length group

    // ── The VARIABLE-LENGTH GROUP's ACTIVATION-BOUNDARY codec (ISO §14.8.2.2 / §14.8.3.2 via §14.9.4.3 SR25;
    //    kb/Work PB204) ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What one member of a variable-length group contributes to its boundary carrier.</summary>
    private enum VarPartKind
    {
        /// <summary>A member with a fixed image — it lands in <c>CobolVarGroup.Fixed</c> at its own offset.</summary>
        Fixed,
        /// <summary>A dynamic-length elementary item — ONE carried component (§8.5.1.10).</summary>
        DynLeaf,
        /// <summary>A dynamic-capacity table — ONE carried component, its occurrences concatenated (§8.5.1.9).</summary>
        DynTable,
        /// <summary>A nested SCALAR variable-length group — FLATTENED: its own fixed run joins ours and its own
        /// components join ours in place, because §8.5.1.12 is stated over relative byte positions and is blind
        /// to the declaration tree.</summary>
        Nested,
    }

    private readonly record struct VarPart(
        VarPartKind Kind, PhysicalModel.Physical Field, DataItem? Item, int FixedWidth, int DynCount);

    /// <summary>A variable-length group's members classified for the boundary carrier, in PHYSICAL order — the
    /// same <see cref="PhysicalModel.PhysicalChildrenOf"/> order and the same dynamic-member selection
    /// <see cref="EmitCurrentImageMethod"/> uses, so the DISPLAY format and the crossing can never disagree
    /// about what a member is.</summary>
    private List<VarPart> VarParts(DataItem group)
    {
        var dyn = group.Children
            .Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)
                && (c.IsDynamicTable || c.IsDynamicLength || (c.IsGroup && !c.IsImageCapable)))
            .ToDictionary(c => c.CsName, StringComparer.Ordinal);
        var parts = new List<VarPart>();
        foreach (var f in phys.PhysicalChildrenOf(group))
        {
            if (!dyn.TryGetValue(f.Name, out var d))
            {
                parts.Add(new VarPart(VarPartKind.Fixed, f, null, f.Width, 0));
                continue;
            }
            parts.Add(d.IsDynamicLength ? new VarPart(VarPartKind.DynLeaf, f, d, 0, 1)
                : d.IsDynamicTable ? new VarPart(VarPartKind.DynTable, f, d, 0, 1)
                : new VarPart(VarPartKind.Nested, f, d, VarFixedWidth(d), VarComponentCount(d)));
        }
        return parts;
    }

    /// <summary>The character width of a variable-length group's FIXED run — its image with every
    /// variable-length component collapsed to nothing. This is the §8.5.1.12.3 accounting the compatibility
    /// relation is stated in ("all dynamic-length elementary items are considered to be of zero length"),
    /// which is exactly why two COMPATIBLE groups lay this string out the same way.</summary>
    public int VarFixedWidth(DataItem group) => VarParts(group).Sum(p => p.FixedWidth);

    /// <summary>How many variable-length components a group carries, nested groups flattened in.</summary>
    public int VarComponentCount(DataItem group) => VarParts(group).Sum(p => p.DynCount);

    /// <summary>Emit a variable-length group's <c>AsVarImage()</c> / <c>FromVarImage()</c> — the boundary codec
    /// §14.8.2.2 and §14.8.3.2 need, the exact analogue of <c>AsImage</c>/<c>FromImage</c> for a group that has
    /// no fixed record window. Gated on <see cref="DataItem.CurrentExtentImageCapable"/>, THE ONE capability
    /// (kb/Work PB204): a group whose extent DISPLAY can compose is a group whose extent a crossing can carry,
    /// and giving the crossing its own gate would be the second copy this repo's two-arm rule forbids.
    /// <para>The fixed members ride exactly the <see cref="AsImageOf"/> / <see cref="EmitMemberFromImage"/>
    /// member-image law the record codec uses — the ONE recipe, no second spelling — at offsets computed with
    /// the variable-length components contributing ZERO, which is §8.5.1.12.3's own convention.</para></summary>
    public void EmitVarImageMethods(DataItem group, CodeWriter w)
    {
        var parts = VarParts(group);
        int totalFixed = parts.Sum(p => p.FixedWidth);
        using (w.Block("public readonly CobolVarGroup AsVarImage()"))
        {
            var fixedParts = new List<string>();
            var dynParts = new List<string>();
            int n = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                        if (p.Field.Width > 0 || p.Field.BitRun is not null) fixedParts.Add(AsImageOf(p.Field));
                        break;
                    case VarPartKind.DynLeaf:
                    case VarPartKind.DynTable:
                        dynParts.Add(CurrentMemberImage(p.Item!));
                        break;
                    case VarPartKind.Nested:
                        // The nested group's own carrier, spliced in place: its fixed run extends ours and its
                        // components take the next p.DynCount slots (the flattening §8.5.1.12 requires).
                        w.Line($"var __n{n} = {p.Field.Name}.AsVarImage();");
                        fixedParts.Add($"__n{n}.Fixed");
                        for (int k = 0; k < p.DynCount; k++) dynParts.Add($"__n{n}.Dyn({k})");
                        n++;
                        break;
                }
            }
            w.Line($"return new CobolVarGroup({(fixedParts.Count > 0 ? string.Join(" + ", fixedParts) : "\"\"")}, "
                + $"new string[] {{ {string.Join(", ", dynParts)} }});");
        }
        // THE FILE-RECORD HALF (determination D-FRA; kb/Work PB981): a record read back as ONE contiguous image
        // (§8.5.1.11.2 — what CurrentImage() wrote) is decomposed into this same carrier by the ONE split rule,
        // CobolVarGroup.FromContiguous, over the component layout below — the flattened VarParts walk, so a
        // nested variable-length group's components are located exactly where FromVarImage's Slice expects them.
        var layout = new List<(int FixedAt, int Unit, long MaxUnits)>();
        ContiguousLayout(group, 0, layout);
        // ⛔ ONE LAYOUT OBJECT PER RECORD TYPE (kb/Work PB1025): the same layout locates a key a variable-length
        // member precedes (CobolContiguousLayout.Position, read through __Contiguous by the SORT/MERGE key and the
        // indexed key registrations), so the decomposition and the key window cannot disagree.
        w.Line($"private static readonly CobolContiguousLayout {RuntimeApi.ContiguousLayoutField} = "
            + RuntimeApi.ContiguousLayoutNew(totalFixed, layout.Select(l => l.FixedAt),
                layout.Select(l => l.Unit), layout.Select(l => l.MaxUnits)) + ";");
        w.Line($"public readonly CobolContiguousLayout {RuntimeApi.ContiguousLayoutProperty} => "
            + $"{RuntimeApi.ContiguousLayoutField};");
        // ⛔ THE EXTENT TABLE (D-FRA (v); kb/Work PB1053): the WRITE / REWRITE / RELEASE side sends where each
        // component of CurrentImage() ends, and the READ / RETURN side decomposes by it when it describes the record
        // received — so every layout round-trips, however many variable-length members flank a fixed one.
        w.Line($"public readonly RecordExtents CurrentExtents() => {RuntimeApi.ContiguousLayoutField}.ExtentsOf(AsVarImage());");
        w.Line($"public void FromContiguousImage(string __r, RecordExtents? __e) => FromVarImage({RuntimeApi.ContiguousLayoutField}.Decompose(__r, __e));");
        using (w.Block("public void FromVarImage(CobolVarGroup __v)"))
        {
            w.Line($"string __s = {RuntimeApi.StrStore("__v.Fixed", $"{totalFixed}")};");
            int off = 0, dynAt = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                        EmitMemberFromImage(p.Field, off, w);
                        off += p.Field.Width;
                        break;
                    case VarPartKind.DynLeaf:
                        // §8.5.1.10.4 — the receiving store for a dynamic-length item: replace, truncate on the
                        // right to the LIMIT, never pad. The component IS the item's whole current content.
                        w.Line($"{p.Field.Name} = {RuntimeApi.DynStore($"__v.Dyn({dynAt})", $"{p.Item!.DynMaxSize}")};");
                        dynAt++;
                        break;
                    case VarPartKind.DynTable:
                        // The capacity comes from the carried length divided by OUR element width — legitimate
                        // because §8.5.1.12.3 admits corresponding tables only "when the byte length of their
                        // elements is equal", which the bind-time compatibility check enforced.
                        // ⛔ WHEN NO COMPONENT WAS CARRIED THIS TABLE IS IN THE EXCESS PART, AND THAT IS A
                        // DIFFERENT RULE (kb/Work PB393). §14.9.25.4 GR9b step 2 sends it to §14.6.9.4, where
                        // "the current capacity of the dynamic table is unaffected, and each element of the
                        // dynamic table is space-filled" — the opposite of recreating it at capacity zero,
                        // which is what an empty CARRIED component means under §14.6.9.2. The shape is
                        // reachable on conforming source: §8.5.1.12.2's last sentence admits a trailing
                        // dynamic-capacity table beyond the shorter group's last character, "treated as if it
                        // corresponds to a space-filled fixed-length table".
                        using (w.Block($"if (__v.HasDyn({dynAt}))"))
                            w.Line($"{p.Field.Name}.FromCurrentImage(__v.Dyn({dynAt}), {p.Field.Width}, "
                                + $"{TableElementFromImage(p.Item!)});");
                        using (w.Block("else"))
                            w.Line($"{p.Field.Name}.SpaceFillElements({p.Field.Width}, "
                                + $"{TableElementFromImage(p.Item!)});");
                        dynAt++;
                        break;
                    case VarPartKind.Nested:
                        w.Line($"{p.Field.Name}.FromVarImage(__v.Slice({off}, {p.FixedWidth}, {dynAt}, {p.DynCount}));");
                        off += p.FixedWidth;
                        dynAt += p.DynCount;
                        break;
                }
            }
        }
    }

    /// <summary>The flattened component layout <c>CobolVarGroup.FromContiguous</c> decomposes a record over:
    /// each variable-length component's offset in the FIXED run, its unit width in characters (one for a
    /// dynamic-length item — <see cref="CurrentMemberImage"/> contributes its content character for character —
    /// the element width for a dynamic-capacity table), and its maximum size in those units (§8.5.1.10.1's
    /// maximum size; the table's maximum capacity). Nested scalar variable-length groups flatten in place, the
    /// same flattening <see cref="VarParts"/> gives the carrier.</summary>
    private void ContiguousLayout(DataItem group, int baseAt, List<(int FixedAt, int Unit, long MaxUnits)> layout)
    {
        int off = baseAt;
        foreach (var p in VarParts(group))
            switch (p.Kind)
            {
                case VarPartKind.Fixed: off += p.Field.Width; break;
                case VarPartKind.DynLeaf: layout.Add((off, 1, p.Item!.DynMaxSize)); break;
                case VarPartKind.DynTable: layout.Add((off, p.Field.Width, p.Item!.OccursSpec?.Max ?? 0)); break;
                case VarPartKind.Nested: ContiguousLayout(p.Item!, off, layout); off += p.FixedWidth; break;
            }
    }

    /// <summary>The inverse of <see cref="CurrentMemberImage"/>'s dynamic-table element lane, arm for arm: a
    /// group element distributes through its own <c>FromImage</c>, a NATIVE numeric element decodes through its
    /// byte-form lane (a float through the distinctly-named IEEE lane), a string-carried element passes
    /// through. The seed occurrence is handed in so a group element keeps the arrays its initializer
    /// allocated. <c>sending: false</c> for the same reason the two <c>FromImage</c> sites below use it
    /// (kb/Work PB230): this is the RECEIVING side of a group decode — the program-visible sending
    /// reference was the write that produced the image, not this redistribution of it into members.</summary>
    private static string TableElementFromImage(DataItem d) =>
        d.IsGroup ? "(__e, __x) => { __e.FromImage(__x); return __e; }"
        : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
            ? (d.Pic.IsFloat
                ? $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImageFloat("__x", d.ProfileName, binary32Carrier: d.Pic.IsSingle)}"
                : $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImage("__x", d.ProfileName, sending: false)}")
            : "(__e, __x) => __x";

    /// <summary>THE ONE RECIPE for what a class pointer/object leaf occupies in a byte image (kb/Work PB231 ->
    /// PB244): <paramref name="width"/> RESERVED placeholder positions, spaces. The storage seed of a
    /// byte-addressed area (<see cref="ImageInitOfOne"/> - D-SLOT) and the one-way transfer image
    /// (<see cref="AsImageOf"/>) both spell it through here, so a strongly-typed group DISPLAYs the same
    /// characters whether it lives in a record struct or in a BASED/ADDRESS-OF storage cell.</summary>
    internal static string SlotPlaceholder(int width) => $"new string(' ', {width})";

    /// <summary>The whole-group image facility of a record struct. <c>AsImage()</c> is emitted for every
    /// <see cref="DataItem.ElementTransferImageCapable"/> group; <c>FromImage</c> (and the bit / national faces)
    /// only for the two-way <see cref="DataItem.ElementImageCapable"/> ones - a group with a class pointer/object
    /// leaf has a one-way image and no inverse (kb/Work PB244; see <see cref="DataItem.TransferImageCapable"/>).</summary>
    public void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = phys.PhysicalChildrenOf(group);
        w.Line($"public readonly string AsImage() => {(members.Count > 0 ? string.Join(" + ", members.Select(AsImageOf)) : "\"\"")};");
        if (!group.ElementImageCapable) return;   // one-way (PB244): no FromImage over a pointer/object leaf
        using (w.Block("public void FromImage(string __s)"))
        {
            w.Line($"__s = {RuntimeApi.StrStore("__s", $"{members.Sum(f => f.Width)}")};");   // pad/truncate to the image width
            int off = 0;
            foreach (var f in members)
            {
                EmitMemberFromImage(f, off, w);
                off += f.Width;
            }
        }
        if (group.GroupUsage is GroupUsage.Bit) EmitBitMethods(group, w);
        if (group.GroupUsage is GroupUsage.National) EmitNatMethods(group, w);
    }

    /// <summary>A NATIONAL GROUP's elementary face (ISO §13.18.29.4 GR2b — "a national group is treated as though
    /// it were an elementary data item of usage national and class and category national described with PICTURE
    /// N(m), where m is the length of the group"; D20/PB79, kb/Work PB327): <c>AsNat()</c> is the group's m
    /// national CHARACTER positions and <c>FromNat</c> stores m of them back. The BYTE form stays
    /// <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image), exactly as it does for a bit
    /// group — two faces of ONE composition, so they cannot disagree.
    /// <para>⛔ DERIVED FROM THE IMAGE, never a second walk over the children: §13.18.29.3 SR3 requires that "all
    /// elementary items subordinate to the subject of the entry shall be explicitly or implicitly described as
    /// usage national", so every byte pair of a national group's image IS a national character position and the
    /// two faces are exactly <c>CobolBits.NatBytes</c> and its inverse. That is why this needs no <c>BitAreaOf</c>
    /// analogue: the bit face exists because §8.5.1.6.3 lets same-level bit items SHARE a byte, and national
    /// positions never share one.</para>
    /// <para>The receiving side fits in POSITIONS before serializing (<c>CobolString.Store</c> then
    /// <c>NatBytes</c>): padding the BYTES with 0x20 would manufacture U+2020 characters instead of the national
    /// spaces §14.9.25.4's national move requires — the identical trap <c>CobolBits.NatWriteWindow</c> documents.</para></summary>
    private static void EmitNatMethods(DataItem group, CodeWriter w)
    {
        // m — the as-if PICTURE N(m) length, THE ONE reader of it (DataItem.AsIfPic reads the same ImageWidth,
        // which stays the CARRIER width: national leaves contribute character positions to it, never bytes).
        int m = group.ImageWidth;
        w.Line($"public readonly string AsNat() => {RuntimeApi.NatReadWindow("AsImage()", "0", $"{m}")};");
        w.Line($"public void FromNat(string __n) => FromImage({RuntimeApi.NatBytes(RuntimeApi.StrStore("__n", $"{m}"))});");
    }

    /// <summary>A BIT GROUP's elementary face (§13.18.29.4 GR1b — "treated as though it were an elementary data item
    /// … PICTURE 1(m), where m is the bit length of the group"; D20/PB79): <c>AsBits()</c> is the group's m
    /// boolean positions and <c>FromBits</c> distributes m boolean positions back to the members. The packed byte
    /// form stays <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image); the two agree because
    /// both compose the same area.
    ///
    /// <para>⛔ BOTH DIRECTIONS PLACE THE MEMBERS BY <see cref="BitLayout.StartBitWithin"/>, and the width is
    /// <see cref="BitLayout.ExtentBits"/> — not a sum of the members' <see cref="BitLayout.RunBits"/>, which is
    /// what both halves used to do. A sum is only right while every member is "immediately following … an item of
    /// the SAME LEVEL" (§8.5.1.6.3's one byte-sharing case); a member at a different level number starts at "the
    /// first bit position of the first available byte" and the skipped bits are implicit filler. MEASURED at
    /// 62a36759 on `01 BG GROUP-USAGE BIT. 05 B1 PIC 1(2) VALUE B"11". 03 B2 PIC 1(2) VALUE B"11".`:
    /// <c>DISPLAY BG</c> printed FOUR boolean positions while <c>FUNCTION LENGTH(BG)</c> — which reads the same
    /// walk through <c>AsIfPic</c> — answered TEN. The elementary face and the length disagreed about the same
    /// group.</para></summary>
    private static void EmitBitMethods(DataItem group, CodeWriter w)
    {
        w.Line($"public readonly string AsBits() => {BitAreaOf(group, BitCarrierOf)};");
        using (w.Block("public void FromBits(string __b)"))
        {
            // pad with boolean zeros / truncate to m (§14.6.8.6)
            w.Line($"__b = {RuntimeApi.StrStoreBoolean("__b", $"{BitLayout.ExtentBits(group)}", justifiedRight: false)};");
            foreach (var m in BitMembers(group))
            {
                int at = BitLayout.StartBitWithin(group, m);
                EmitRunMemberFromBits(m, "__b", at < 0 ? 0 : at, w);
            }
        }
    }

    /// <summary>The members of <paramref name="group"/> that OCCUPY storage — a redefining child overlays its
    /// target and adds none (§13.18.44), and a non-data child (a condition-name entry) is not a member at all.
    /// The one member list for every bit composition below, so they cannot walk different populations.</summary>
    private static IEnumerable<DataItem> BitMembers(DataItem group) =>
        group.Children.Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary));

    /// <summary>⛔ THE ONE §8.5.1.6.3 AREA COMPOSITION for a bit group: each member's boolean carrier placed at
    /// the bit position the walk gives it, with the implicit-filler zeros between and after them, for the
    /// group's whole <see cref="BitLayout.ExtentBits"/> — §13.18.29.4 GR1b's "PICTURE 1(m), where m is the bit
    /// length of the group". <paramref name="carrierOf"/> is what a member contributes: its FIELD for the
    /// emitted <c>AsBits()</c> face, its INITIAL value for the compile-time image seed, and the two therefore
    /// cannot place the same member at two different positions.
    ///
    /// <para>The filler positions are §8.5.1.6.3's — "implicit filler bit positions are generated … as needed to
    /// advance alignment to a required natural boundary for the next item within that group" and, at the end,
    /// "as needed to increase the number of bits to fill an integral number of characters". They are ZEROS
    /// because that is the boolean initial state §13.18.63 gives a position no VALUE reaches, and
    /// <c>CobolBits.Pack</c>'s own contract already zero-fills a trailing partial byte.</para></summary>
    private static string BitAreaOf(DataItem group, Func<DataItem, string> carrierOf)
    {
        var parts = new List<string>();
        int at = 0;
        foreach (var m in BitMembers(group))
        {
            int start = BitLayout.StartBitWithin(group, m);
            if (start < 0) start = at;      // an unmodelled overlay chain — keep the composition total
            if (start > at) parts.Add(EmitText.CsLiteral(new string('0', start - at)));
            parts.Add(carrierOf(m));
            at = start + BitLayout.RunBits(m);
        }
        int total = BitLayout.ExtentBits(group);
        if (total > at) parts.Add(EmitText.CsLiteral(new string('0', total - at)));
        return parts.Count > 0 ? string.Join(" + ", parts) : "\"\"";
    }

    /// <summary>One bit member's INITIAL boolean carrier — the compile-time twin of <see cref="BitCarrierOf"/>,
    /// for the image seed: a nested bit group composes its own area (the same <see cref="BitAreaOf"/> walk, so
    /// §8.5.1.6.3 applies "within that group" at every level), a bit leaf stores its VALUE into its declared
    /// boolean positions (§14.6.8.6 — zero pad, truncate right) or takes the all-zero boolean initial state, and
    /// a fixed-OCCURS member repeats that for every occurrence (§13.18.63.4 GR9).</summary>
    private string InitialBitCarrierOf(DataItem m, bool useValues, Subscripts subs = default)
    {
        // ⛔ THE BIT CARRIER IS THE THIRD STORAGE LANE, AND IT DROPPED THE FORMAT 2 (table) VALUE TOO
        // (kb/Work PB505 — the same shape kb/Work PB208 found on the character-image lane). It read `m.RawValue`,
        // which is NULL for a table VALUE, and then repeated ONE occurrence carrier `Occurs` times, so every
        // occurrence of a USAGE BIT table inside a bit group came back all-zero. MEASURED before the fix on
        // `01 BG GROUP-USAGE BIT. 05 B PIC 1(4) OCCURS 3 VALUES ARE B"1010" B"0101" FROM (1). 01 BV REDEFINES
        // BG PIC X(2).`: [0000|0000|0000], where the SAME declaration without the alias — the record-struct
        // lane — gave [1010|0101|1010]. §13.18.63.4 GR12/GR13 govern this lane exactly as they govern the other
        // two, so it asks DataItem.ValueAt with the occurrence tuple like they do.
        if (useValues && m.ContainsTableValue && m.Occurs is { } occ and > 0)
            return "(" + string.Join(" + ", Enumerable.Range(1, occ)
                .Select(o => OneBitCarrierOf(m, useValues, subs.With(o)))) + ")";
        string one = OneBitCarrierOf(m, useValues, m.Occurs is > 0 ? subs.With(1) : subs);
        return m.Occurs is { } n and > 1 ? RuntimeApi.StrRepeat(one, $"{n}") : one;
    }

    /// <summary>ONE occurrence of a bit run member’s initial carrier — see <see cref="InitialBitCarrierOf"/>,
    /// which repeats or composes it per occurrence.</summary>
    private string OneBitCarrierOf(DataItem m, bool useValues, Subscripts subs)
    {
        if (m.IsGroup) return BitAreaOf(m, c => InitialBitCarrierOf(c, useValues, subs));
        // A member the binder failed to describe still has to occupy its positions, or every member after
        // it is displaced; WidthBits is asked ONLY here, because for a group it is a whole ExtentBits walk.
        if (m.Pic is not { } pic) return EmitText.CsLiteral(new string('0', BitLayout.WidthBits(m)));
        if (!useValues || m.ValueAt(subs) is not { } raw) return EmitText.CsLiteral(new string('0', pic.Length));
        // ⛔ THE VALUE RULES ARE ASKED OF THE ONE READER, NOT RESTATED HERE (kb/Work PB584). This arm used to be
        // `FigurativeInitializer(raw, pic) ?? StrStoreBoolean(Decode(raw), …)` — the FIGURATIVE arm and the
        // PLAIN-LITERAL arm, with §8.3.3.6.4 GR2's Format-6 `ALL literal-1` arm missing, which the record-struct
        // lane (ValueInitializer.InitializerFor) has always had. MEASURED at 2f6b38c61 on
        // `01 BG GROUP-USAGE BIT. 05 K1 PIC 1(4) VALUE ALL B"1". 05 K2 PIC 1(4) VALUE ZERO. 01 BV REDEFINES BG
        // PIC X(1).`: K1 read back [0000] through the alias and [1111] without it — the two-arm shape, one arm
        // short, in the lane nothing but an alias reaches.
        return vals.BooleanCarrierOf(raw, pic);
    }

    /// <summary>Distribute one run member's slice of an unpacked bit carrier — THE one distributor, ridden by
    /// both the bit-group <c>FromBits</c> face and the record <c>FromImage</c> run loop (kb/Work PB161: the
    /// FromImage copy lacked the OCCURS loop its sibling here had, so a <c>PIC 1(8) USAGE BIT OCCURS 3</c>
    /// member made the generated record fail backend compilation with CS0029 — the sixth instance of the
    /// two-arm-dispatch defect shape).</summary>
    private static void EmitRunMemberFromBits(DataItem m, string carrier, int at, CodeWriter w)
    {
        int per = m.IsGroup ? m.AsIfPic!.Length : m.Pic!.Length;
        if (m.Occurs is { } o)
        {
            using (w.Block($"for (int __i = 0; __i < {o}; __i++)"))
                w.Line(m.IsGroup
                    ? $"{m.CsName}[__i].FromBits({RuntimeApi.BitsSlice(carrier, $"{at} + __i * {per}", $"{per}")});"
                    : $"{m.CsName}[__i] = {RuntimeApi.BitsSlice(carrier, $"{at} + __i * {per}", $"{per}")};");
        }
        else
        {
            w.Line(m.IsGroup
                ? $"{m.CsName}.FromBits({RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")});"
                : $"{m.CsName} = {RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")};");
        }
    }

    /// <summary>One member's AsImage sub-expression: a scalar string field directly, a nested group's
    /// <c>AsImage()</c>, a NATIVE fixed-point leaf the BYTES it occupies (<c>CobolNum.FormatImage</c> with the
    /// leaf's own profile — its zoned digits for USAGE DISPLAY, its radix-2 / BCD bytes for BINARY / PACKED,
    /// §13.18.60.4 GR4/GR11), or — for a fixed-OCCURS table — the concatenation of every occurrence's image
    /// (ISO §14.9: a group move treats the whole group, INCLUDING every OCCURS position, as one alphanumeric
    /// item).</summary>
    /// <summary>A §8.5.1.6.3 run member's BIT CARRIER expression — THE one carrier law (kb/Work PB161): a bit
    /// leaf's '0'/'1' string field; a bit GROUP's <c>AsBits()</c>; a fixed-OCCURS member the concatenation of
    /// every occurrence's carrier (the AsImage run packer read the raw <c>string[]</c> field before, CS1503).</summary>
    private static string BitCarrierOf(DataItem m) =>
        m.Occurs is not null
            ? (m.IsGroup ? $"string.Concat(System.Array.ConvertAll({m.CsName}, __e => __e.AsBits()))"
                         : $"string.Concat({m.CsName})")
            : m.IsGroup ? $"{m.CsName}.AsBits()" : m.CsName;

    private static string AsImageOf(PhysicalModel.Physical f) =>
        // D19/PB43 — a USAGE BIT run images as its PACKED bits (§13.18.60.4 GR5), high-order first, and a run may
        // span several FIELDS because §8.5.1.6.3 puts same-level bit items at successive bit positions. The run's
        // leader packs every member's carrier concatenated; a continuation contributed Width 0 and images as "".
        f.BitRun is { } run
            ? RuntimeApi.BitsPack(string.Join(" + ", run.Select(BitCarrierOf)),
                                  $"{run.Sum(BitLayout.RunBits)}")
        : f.Width == 0 ? "\"\""
        // A class pointer/object leaf (kb/Work PB244) contributes its reserved placeholder positions, every
        // occurrence at once (Width is already per-occurrence width x OCCURS). Reached only through the one-way
        // AsImage of a TransferImageCapable group - FromImage is never emitted over such a leaf.
        : f.SlotLeaf ? SlotPlaceholder(f.Width)
        // ⛔ A NATIONAL LEAF IMAGES AS ITS BYTES (kb/Work PB327): two per character position, high-order first
        // (ISO §13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two, UTF-16BE), through the ONE
        // serializer CobolBits.NatBytes that the Tier-B window, the EXTERNAL/BASED cell seed and CONVERT's
        // raw-storage channel already ride. Concatenating the occurrences BEFORE serializing is the same string
        // as serializing each and concatenating, so the OCCURS arm needs no separate spelling.
        // ⛔ IT IS A TRANSFORM OVER THE CARRIER'S OWN CHARACTER IMAGE, NOT AN ALTERNATIVE TO IT (design D-N7,
        // kb/Work PB646). A national leaf's category decides what its characters ARE — for the category-national
        // and boolean forms the carrier IS those characters, and for the national-form NUMERIC of §13.18.60.3
        // SR12 they are the zoned digit run CobolNum.FormatImage writes from the native carrier — while the
        // USAGE decides only how a character is SERIALIZED. Composing the two is what let SR12's numeric shapes
        // land without a second numeric byte form; the two used to be mutually exclusive arms, because the
        // national-form numeric was staged loud at PictureAnalyzer and could not reach emit.
        : f.NatLeaf is not null ? RuntimeApi.NatBytes(CarrierImageOf(f)) : CarrierImageOf(f);

    /// <summary>The field's contribution to its group's image in its own CHARACTER alphabet — a nested group's
    /// <c>AsImage()</c>, a native fixed-point leaf's zoned/radix-2/BCD/IEEE image through its own profile, else the
    /// carrier string verbatim. <see cref="AsImageOf"/> serializes the result (a national leaf's characters become
    /// UTF-16BE byte pairs; every other leaf's are its bytes already).</summary>
    private static string CarrierImageOf(PhysicalModel.Physical f) =>
        f.Occurs == 0
            ? (f.IsGroupStruct ? $"{f.Name}.AsImage()"
               // A FLOAT leaf encodes through the IEEE lane (kb/Work PB164 wave 2 — distinctly named so
               // integer call sites stay unambiguous).
               : f.NumLeaf is { } leaf ? (leaf.Pic!.IsFloat
                    ? RuntimeApi.NumFormatImageFloat(f.Name, leaf.ProfileName, leaf.Pic.IsSingle)
                    : RuntimeApi.NumFormatImage(f.Name, leaf.ProfileName))
               : f.Name)
        : f.IsGroupStruct ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => __e.AsImage()))"
        : f.NumLeaf is { } l ? $"string.Concat(System.Array.ConvertAll({f.Name}, __e => {(l.Pic!.IsFloat
                ? RuntimeApi.NumFormatImageFloat("__e", l.ProfileName, l.Pic.IsSingle)
                : RuntimeApi.NumFormatImage("__e", l.ProfileName))}))"
        : $"string.Concat({f.Name})";

    /// <summary>Distribute the slice of the image at <paramref name="off"/> into one member: a scalar string field
    /// gets its substring; a nested group gets <c>FromImage</c>; a NATIVE fixed-point leaf decodes its byte slice
    /// (<c>CobolNum.ParseImage</c> with the leaf's own profile, cast to its CLR storage type — an incompatible
    /// position, e.g. the spaces a short record's pad legitimately deposits, decodes deterministically per ISO
    /// §14.6.13.2, see CobolNum); a fixed-OCCURS table loops its occurrences, each taking its per-occurrence width
    /// in source order (the array elements are value-type structs/strings, mutated in place).
    /// <para>ONE profile, never an image-specific override: since the image IS the item's bytes, a BINARY/PACKED
    /// leaf's sign lives in those bytes (two's complement / the sign nibble) and its <c>SignKind</c> — a DISPLAY
    /// concern — is not consulted at all. The former <c>ImageProfileOf</c> sign rewrite existed only because the
    /// image was a zoned digit run that had to carry a fixed-width sign.</para></summary>
    private static void EmitMemberFromImage(PhysicalModel.Physical f, int off, CodeWriter w)
    {
        // D19/PB43 — the run's leader unpacks the shared byte(s) ONCE and distributes the boolean positions back
        // to each member in declaration order; the continuations have Width 0 and consume no slice.
        if (f.BitRun is { } run)
        {
            int runBits = run.Sum(BitLayout.RunBits);
            // ⚠ The carrier local is named for the run's OFFSET, not a bare `__bits`: a group may hold SEVERAL
            // runs (two bit items separated by a character item are two runs, §8.5.1.6.3 — the character item
            // breaks the same-level adjacency), and they all land in this one method scope. The first version
            // emitted `var __bits` per run and a two-run group failed to compile with CS0128.
            string carrier = $"__bits{off}";
            w.Line($"var {carrier} = {RuntimeApi.BitsUnpack($"__s.Substring({off}, {f.Width})", $"{runBits}")};");
            int at = 0;
            foreach (var m in run)
            {
                // THE one distributor (kb/Work PB161) — a bit GROUP member spreads to its subordinates
                // (FromBits, D20/PB79), a fixed-OCCURS member loops its occurrences.
                EmitRunMemberFromBits(m, carrier, at, w);
                at += BitLayout.RunBits(m);
            }
            return;
        }
        if (f.Width == 0) return;   // a run continuation — its value came from the leader's unpack
        // ⛔ THE INVERSE OF AsImageOf's national arm (kb/Work PB327): decode the UTF-16BE byte pairs back to the
        // leaf's character positions through CobolBits.NatReadWindow — the inverse CobolBits.NatBytes names, never
        // a second decoder. A pair the image is too short to hold decodes as the NATIONAL space, which is exactly
        // §14.9.30.4 GR15's fill for a national record area ("a trailing space is defined to be the national space
        // character") — the connector's byte-level pad and this decode agree on it by construction.
        if (f.NatLeaf is { } nat)
        {
            // ⛔ THE INVERSE COMPOSITION OF AsImageOf's national arm (design D-N7, kb/Work PB646): the byte
            // pairs decode to the leaf's CHARACTER positions, and the carrier decoder below turns those
            // characters into the carrier — a string for the category-national/national-edited/boolean forms,
            // the native fixed-point value for §13.18.60.3 SR12's national-form NUMERIC. Reading the window and
            // then NOT decoding the digits is what a single mutually-exclusive arm did, and it stored a string
            // into a `long` field (uncompilable generated C#, unreachable only while the category was staged).
            int pos = NationalWindow.PositionsOf(nat)!.Value;
            if (f.Occurs == 0)
            {
                w.Line($"{f.Name} = {CarrierFromChars(f, RuntimeApi.NatReadWindow("__s", $"{off}", $"{pos}"))};");
                return;
            }
            using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
                w.Line($"{f.Name}[__i] = {CarrierFromChars(f, RuntimeApi.NatReadWindow("__s",
                    $"{off} + __i * {RuntimeApi.BytesPerNational * pos}", $"{pos}"))};");
            return;
        }
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : $"{f.Name} = {CarrierFromChars(f, $"__s.Substring({off}, {f.Width})")};");
            return;
        }
        int elem = f.Width / f.Occurs;   // per-occurrence width (Width = elem × Occurs, exact by construction)
        using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
            w.Line(f.IsGroupStruct
                ? $"{f.Name}[__i].FromImage(__s.Substring({off} + __i * {elem}, {elem}));"
                : $"{f.Name}[__i] = {CarrierFromChars(f, $"__s.Substring({off} + __i * {elem}, {elem})")};");
    }

    /// <summary>⛔ THE ONE carrier decoder — the inverse of <see cref="CarrierImageOf"/>: the member's CHARACTER
    /// image (<paramref name="chars"/>) as its stored carrier. A native fixed-point leaf decodes through its own
    /// profile and is cast to its CLR storage type; a FLOAT leaf takes the IEEE bit-reinterpretation lane, because
    /// the Int128 lane's cast would numerically CONVERT the parsed integer (kb/Work PB164 wave 2); every
    /// string-shaped field is its characters already. An incompatible position — e.g. the spaces a short record's
    /// pad legitimately deposits — decodes deterministically per ISO §14.6.13.2 (see CobolNum).
    /// <para>It is ONE function because it was three copies, and the national arm was a fourth that decoded
    /// nothing (kb/Work PB646).</para></summary>
    private static string CarrierFromChars(PhysicalModel.Physical f, string chars) =>
        f.NumLeaf is { } leaf
            ? $"({leaf.Pic!.ClrType}){(leaf.Pic!.IsFloat
                    ? RuntimeApi.NumParseImageFloat(chars, leaf.ProfileName, binary32Carrier: leaf.Pic.IsSingle)
                    : RuntimeApi.NumParseImage(chars, leaf.ProfileName, sending: false))}"
            : chars;
}
