// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>WHICH RULE a data-division seed is composed by (kb/Work PB1267). The same composition — one element,
/// its members in order, the VALUE recipe at each leaf — serves two occasions the standard words differently:
/// <list type="bullet">
/// <item><see cref="InitialState"/> — §14.6.2.3.2's initial state: the OPTIONS-paragraph background first
/// (action 1), then every VALUE clause, a group-level VALUE included (§13.18.63.4 GR5).</item>
/// <item><see cref="Initialize"/> — the new element of a dynamic-capacity table whose OCCURS carries INITIALIZED:
/// §8.5.1.9.5, "as though they had been the subject of a statement of the form INITIALIZE … WITH FILLER ALL TO
/// VALUE THEN TO DEFAULT" — and the whole content of a CONSTANT RECORD, §13.18.15.4 GR1 (kb/Work PB1233,
/// chosen once at the root by <see cref="ValueInitializer.RecipeFor"/>). §14.9.20.4 GR5 c) 1. b qualifies a leaf by "a data-item format VALUE clause … in the
/// data description entry of the ELEMENTARY data item" (so a group-level VALUE is not an INITIALIZE sender), GR6
/// a) 3. makes that leaf's sender "a literal that, when moved to the receiving-operand with a MOVE statement,
/// produces the same result as the initial value of the data item as produced by the application of the VALUE
/// clause" (so a VALUE-bearing leaf composes exactly as it does in the initial state), and GR6 c)'s table gives
/// every other leaf its category's figurative constant — ZEROES for a numeric-edited item, which the initial
/// state leaves to the background (spaces).</item>
/// </list></summary>
internal enum SeedRecipe
{
    /// <summary>§14.6.2.3.2 — the background, then every VALUE clause.</summary>
    InitialState,
    /// <summary>§8.5.1.9.5 — INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT.</summary>
    Initialize,
}

/// <summary>The VALUE-or-default initializer rendering of the DATA DIVISION (P7 Step 9l): an OCCURS table's
/// array literal, a group's composed initializer (via <see cref="GroupValueSlicer"/>), an elementary item's
/// VALUE / figurative / COBOL default. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class ValueInitializer(EmitContext ctx)
{
    /// <summary>The group-VALUE positional distributor (set once by <see cref="DataEmitter"/>).</summary>
    public GroupValueSlicer Slicer { get; set; } = null!;

    /// <summary>THE §14.6.2.3.2 action-1 background (kb/Work PB152) — shared with <see cref="GroupImageCodec"/>
    /// so the native-field arm and the Tier-B image arm cannot answer "what fills a VALUE-less item" differently.
    /// One instance per emitter so its section-root sets are built once.</summary>
    public InitialStateBackground Background { get; } = new(ctx);

    /// <summary>⛔ THE ONE CHOICE OF SEED RECIPE FOR A ROOT (kb/Work PB1233) — asked at the two roots every seed
    /// enters through, <see cref="FieldInit"/> (the record-struct lane) and <see cref="GroupImageCodec.ImageInitOf"/>
    /// (the image lane), so the lanes cannot answer it differently. A record described with CONSTANT RECORD has
    /// the content "as though the clause had been omitted and the record had been the subject of an INITIALIZE
    /// statement that is specified with" the FILLER phrase, the VALUE phrase with ALL and the DEFAULT phrase
    /// (§13.18.15.4 GR1) — i.e. <see cref="SeedRecipe.Initialize"/>, not the initial state: a VALUE-less numeric-
    /// edited item takes the edited ZERO of §14.9.20.4 GR6 c), no OPTIONS INITIALIZE background reaches it (the
    /// INITIALIZE overwrites it), and a group-level VALUE is no sender (GR5 c) 1. b). The flag lives on the
    /// level-01 root (<see cref="DataItem.IsConstantRecord"/>) and a root is where every seed starts, so the
    /// choice is made once here and rides the <paramref name="requested"/> recipe down the subtree.</summary>
    internal static SeedRecipe RecipeFor(DataItem root, SeedRecipe requested) =>
        root.IsConstantRecord ? SeedRecipe.Initialize : requested;

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.
    /// <para><paramref name="outer"/> is the OCCURRENCE CONTEXT — the subscripts of every OCCURS level already
    /// entered on the way down from the record root, most inclusive first. It is empty for a level-01/77 field and
    /// grows by one at each OCCURS entry, so at any leaf it is exactly the tuple §13.18.63.3 SR20 keys a Format 2
    /// (table) VALUE by, and <see cref="DataItem.ValueAt"/> can answer "what initializes THIS occurrence" without
    /// the caller knowing which entry in the chain wrote the clause (kb/Work PB505). Threading it is what lets a
    /// table VALUE live on an entry SUBORDINATE to the OCCURS (SR18) and span several dimensions (GR12's
    /// odometer); before it, both were refused.</para></summary>
    /// <param name="recipe">The rule this seed is composed by (<see cref="SeedRecipe"/>) — the initial state
    /// everywhere except inside the INITIALIZED seed of a dynamic-capacity table's new elements.</param>
    public string FieldInit(DataItem item, Subscripts outer = default, SeedRecipe recipe = SeedRecipe.InitialState)
    {
        recipe = RecipeFor(item, recipe);
        // A DYNAMIC-capacity table (§13.18.38 Format 4, D9): an out-of-line CobolDynTable seeded per occurrence with
        // the SAME one-occurrence initializer the fixed path repeats (heed DEVLOG 643 — seed EVERY occurrence). Opens
        // at FROM (min), raised to the §13.18.63.4 GR16 initial capacity when a table VALUE applies; TO is the
        // expected capacity.
        // ⛔ TWO SEEDS WHEN INITIALIZED IS WRITTEN (kb/Work PB1267). The occurrences the table OPENS with belong to
        // the initial state (this call's own recipe); an element a STATEMENT creates later belongs to §8.5.1.9.5 —
        // "any elementary items not referenced as receiving operands in a statement that creates new elements in
        // that table are first initialized as though they had been the subject of a statement of the form
        // INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT" — so it takes the SeedRecipe.Initialize
        // composition. The two differ on a VALUE-less numeric-edited leaf (edited ZERO vs the background), under an
        // OPTIONS INITIALIZE fill, and under a group-level VALUE; where they compose the same text the second seed
        // is omitted (null), so the common table carries one lambda. Without INITIALIZED "the content of any
        // unreferenced locations in the new table element and the content of the other new occurrences are
        // undefined", and the initial-state seed is a conforming choice for it (docs/CONFORMANCE.md DOC-A.1-61).
        if (item.IsDynamicTable)
            return DynTableNew(item, item.ElementType, (o, r) => ElementInit(item, outer.With(o), r), recipe,
                valuesSeedInitialState: true);
        if (item.Occurs is { } n)
        {
            // Without a table VALUE in the subtree every occurrence is identical — compose ONE and repeat it (the
            // shape both lanes always had, and the reason a 49-level CCVS record still emits in linear time).
            if (!item.ContainsTableValue)
                return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(ElementInit(item, outer.With(1), recipe), n))} }}";
            return $"new {item.ElementType}[] {{ "
                 + string.Join(", ", Enumerable.Range(1, n).Select(o => ElementInit(item, outer.With(o), recipe))) + " }";
        }
        return ElementInit(item, outer, recipe);
    }

    /// <summary>ONE table element / one non-table field at <paramref name="subs"/>: a group composes its members
    /// (or takes its own §13.18.63.4 GR5 area VALUE), an elementary item takes the literal its VALUE clause gives
    /// this occurrence.</summary>
    private string ElementInit(DataItem item, Subscripts subs, SeedRecipe recipe) =>
        item.IsGroup ? Slicer.ComposedInit(item, subs, recipe) : InitializerFor(item, subs, recipe);

    /// <summary>⛔ THE ONE CONSTRUCTION OF A DYNAMIC-CAPACITY TABLE, from the table's OWN OCCURS clause (kb/Work PB1118's
    /// invariant, <c>DynamicCapacityAgreementDriftTests</c>) — for both storage lanes: a record struct's typed
    /// <c>CobolDynTable&lt;T&gt;</c> (<see cref="FieldInit"/>) and a cell-backed area's table of element cells
    /// (<c>GroupImageCodec.CellDynSeeds</c>; kb/Work PB1042). <paramref name="elementAt"/> composes occurrence
    /// <c>o</c>'s element under a recipe. It opens at FROM (min), raised to the §13.18.63.4 GR16 initial capacity when
    /// a table VALUE seeds the initial state (<paramref name="valuesSeedInitialState"/> false only for a PLAIN external
    /// record, §13.18.63.4 GR4 a) — "If more than one VALUE clause applies, the maximum value thus calculated becomes
    /// the initial capacity"; occurrences within it take their keyed element, and growth beyond re-seeds through the
    /// same function to the VALUE-less default); TO is the expected capacity; an occurrence a statement creates takes
    /// the INITIALIZED phrase's composition when it differs — keyed to ITS occurrence whatever the initial state did,
    /// because §8.5.1.9.5's INITIALIZE … TO VALUE sends "the literal in the VALUE clause that corresponds to the
    /// occurrence being initialized" (§14.9.20.4 GR6 a) 3.).</summary>
    internal static string DynTableNew(DataItem item, string elementType, Func<int, SeedRecipe, string> elementAt,
                                       SeedRecipe recipe, bool valuesSeedInitialState)
    {
        var s = item.OccursSpec!;
        int min = s.InitialCap ?? 0;
        string expected = s.ExpectedMax is int e ? e.ToString() : "null";
        bool openingKeyed = valuesSeedInitialState && item.ContainsTableValue;
        if (!openingKeyed && !(s.Initialized && item.ContainsTableValue))
        {
            string opening = elementAt(1, recipe);
            string created = s.Initialized ? elementAt(1, SeedRecipe.Initialize) : opening;
            return $"new CobolDynTable<{elementType}>(() => {opening}, {min}, {expected}, "
                 + (created == opening ? "null" : $"() => {created}") + ")";
        }
        int cap = Math.Max(min, item.TableValueInitialCapacity ?? min);
        string openingAt = openingKeyed ? OccurrenceSwitch(o => elementAt(o, recipe), cap) : $"(int __i) => {elementAt(0, recipe)}";
        string createdAt = s.Initialized ? OccurrenceSwitch(o => elementAt(o, SeedRecipe.Initialize), cap) : openingAt;
        return $"new CobolDynTable<{elementType}>({openingAt}, {min}, {expected}, "
             + $"{(createdAt == openingAt ? "null" : createdAt)}, {(openingKeyed ? cap : min)})";
    }

    /// <summary>⛔ THE ONE per-occurrence seed switch (kb/Work PB1042 made it shared): <paramref name="elementAt"/>
    /// composes the element of occurrence <c>o</c> — the record-struct lane's typed element, or a cell-backed area's
    /// element cell (<c>GroupImageCodec.CellDynSeeds</c>) — and the switch (<c>(int __i) =&gt; __i switch { … }</c> over 1..<paramref name="cap"/>) folds IDENTICAL arms into one, since the common case is one literal over a whole range. Occurrence 0
    /// identifies no table element (§13.18.63.3 SR20 admits none below 1), so it is the tuple that deliberately
    /// matches no FROM..TO range: the element as it stands with no table VALUE keyed to it.</summary>
    internal static string OccurrenceSwitch(Func<int, string> elementAt, int cap)
    {
        string dflt = elementAt(0);
        var arms = new List<string>();
        var pending = new List<int>();
        string? pendingText = null;
        void Flush()
        {
            if (pendingText is null || pendingText == dflt) { pending.Clear(); pendingText = null; return; }
            arms.Add($"{string.Join(" or ", pending)} => {pendingText},");
            pending.Clear();
            pendingText = null;
        }
        for (int o = 1; o <= cap; o++)
        {
            string text = elementAt(o);
            if (pendingText is not null && !string.Equals(text, pendingText, StringComparison.Ordinal)) Flush();
            pendingText = text;
            pending.Add(o);
        }
        Flush();
        return arms.Count == 0
            ? $"(int __i) => {dflt}"
            : $"(int __i) => __i switch {{ {string.Join(" ", arms)} _ => {dflt} }}";
    }
    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    public string InitializerFor(DataItem item, Subscripts subs = default, SeedRecipe recipe = SeedRecipe.InitialState)
        // ⛔ THE ONE READER for "what initializes this item at this occurrence" — DataItem.ValueAt: the Format-1
        // VALUE (the same for every occurrence, §13.18.63.4 GR9) or the Format-2 literal keyed to this subscript
        // tuple (GR12–GR15). The image lane asks the same property, so the two cannot disagree.
        => InitializerFrom(item, item.ValueAt(subs), recipe);

    /// <summary>⛔ THE ONE NO-VALUE SEED, both lanes, both recipes (kb/Work PB1267 over PB152's choke point): what
    /// a VALUE-less elementary item starts as, or null for the caller's own category baseline (spaces / zero /
    /// boolean zeros / null — which IS §14.9.20.4 GR6 c)'s figurative constant for every category but one).
    /// <see cref="SeedRecipe.InitialState"/> asks the §14.6.2.3.2 background; <see cref="SeedRecipe.Initialize"/>
    /// never does (an INITIALIZE sends GR6 c)'s constant, not the OPTIONS fill) and answers the one category whose
    /// constant the baseline does not already spell: a numeric-edited item receives "Figurative constant ZEROES"
    /// by MOVE, i.e. the EDITED image of zero (BLANK WHEN ZERO included) — the compile-time compose
    /// <see cref="EditedImageOfNumericValue"/> the VALUE clause uses, or the run-time locale compose for a
    /// format-2 (LOCALE) item.</summary>
    public string? NoValueSeed(DataItem item, PicInfo pic, SeedRecipe recipe)
    {
        if (recipe is SeedRecipe.InitialState) return Background.Seed(item, pic);
        if (pic.Category is not PicCategory.NumericEdited) return null;
        if (pic.LocaleEdit is not null) return RuntimeApi.LocaleEditCompose(pic, Int128.Zero, 0, item.BlankWhenZero);
        // A figurative ZERO moved to a numeric-edited item is the numeric value zero (§14.9.25.4), at every
        // edition — hence the literal "0" at the 2023 level, where the VALUE rule treats the two identically.
        return EditedImageOfNumericValue(2023, item, pic, "0") is { } z
            ? EmitText.CsLiteral(z) : null;
    }

    /// <summary>⛔ THE ONE §13.18.63 VALUE RECIPE, with the operand handed in rather than read off the item —
    /// the arm the REPORT SECTION needs (kb/Work PB506). A format-4 VALUE operand lives on the report field
    /// model, not on the synthetic printable item, and a multi-operand clause gives a DIFFERENT operand to each
    /// repetition (§13.18.63.4 GR23), so there is no single <see cref="DataItem.ValueAt"/> to ask; everything
    /// after that lookup — alignment, figuratives, ALL, the numeric-edited compose, the image encodings — is the
    /// same rule and is therefore the same code.
    /// <para>That identity is the POINT, not a convenience. §13.18.63.4 GR21 imports GR7 into format 4 ("Each
    /// literal is aligned in the associated data item in accordance with 14.6.8 … except that initialization is
    /// not affected by a JUSTIFIED clause and no editing takes place") and GR8 (BLANK WHEN ZERO has no effect
    /// when the literal is alphanumeric or national), and §13.18.63.3 SR34 imports SR11 ("Editing characters in
    /// a picture character-string for an alphanumeric-edited or national-edited data item do not cause editing
    /// of the initial value"). The report emitter used to push a VALUE operand through the SOURCE clause's
    /// implicit MOVE (§13.18.53.4 GR1) instead, which applies exactly the three transforms those rules exclude:
    /// `PIC XXBXX VALUE "AB CD"` printed `AB  C`, `PIC X(5) JUSTIFIED VALUE "AB"` printed `   AB`, and
    /// `PIC ZZZ9 BLANK WHEN ZERO VALUE "0000"` printed spaces — each while the IDENTICAL working-storage entry,
    /// through this method, was right.</para></summary>
    public string InitializerFrom(DataItem item, string? effRaw, SeedRecipe recipe = SeedRecipe.InitialState)
    {
        // ⛔ THE ONE CATEGORY/WIDTH READER (DataItem.OperandPic, D20), never raw `Pic` — which is NULL for every
        // GROUP. An elementary item reads identically (OperandPic IS Pic there), and the two group shapes the
        // standard admits as VALUE-clause receivers arrive described rather than crashing: a bit / national group
        // through its §13.18.29.4 GR1b/GR2b as-if PICTURE 1(m) / N(m), an ORDINARY group through the VALUE
        // clause's OWN rule for that subject — §13.18.63.3 SR4. §14.9.39.4 GR6 places a
        // level-88 literal in the conditional variable "according to the rules for the VALUE clause" and names a
        // group conditional variable explicitly, so the group arms are THIS recipe's business and not a second
        // copy of it in the SET emitter (kb/Work PB560).
        var pic = item.OperandPic ?? AsIfAlphanumericGroup(item);

        // A DYNAMIC LENGTH item (ISO §8.5.1.10 / §13.18.19): the field is a native string. §8.6.4 — a VALUE clause
        // defines the initial length (MOVE-like, §13.18.63.4 GR7; stored truncated on the right to the LIMIT, no
        // padding); ABSENT a VALUE the initial length is zero (§8.6.4 second sentence — never a fixed-width fill).
        // A figurative VALUE other than `ALL literal` has length ONE (§8.3.3.6.4 GR3b — FigurativeInitializer fills
        // pic.Length = 1 for the single-symbol X/N picture), so it initializes to a single fill character, NOT "".
        if (item.IsDynamicLength)
        {
            // ⛔ §13.4.4 GR1 (kb/Work PB1241): "A data-item format or table format VALUE clause specified in the file
            // section is ignored except in the execution of the INITIALIZE statement", and "the initial length of a
            // dynamic-length elementary item is zero". §8.6.4 is the general rule, and the file section overrides it —
            // an INITIALIZE ... TO VALUE reaches its own recipe, not this initial-state reader.
            if (effRaw is not { } dv || InFileSection(item)) return "\"\"";
            if (FigurativeInitializer(dv, pic) is { } figFill) return figFill;
            return RuntimeApi.DynStore(EmitText.CsLiteral(CobolLiteral.Decode(dv)), item.DynMaxSize.ToString());
        }

        // CCVS leniency: an ALPHANUMERIC literal VALUE on a NUMERIC item is read AS the numeric literal ISO
        // §13.18.63.3 SR2 asked for (the 85 corpus writes `PIC 999 VALUE "000"` — NC107A's DATA-P; DataBinder's
        // ValidateValueCategory diagnoses it, COBOLNET1657, and rewrites it to the number under --permissive).
        // ⛔ ONE RECIPE FOR BOTH STORAGE AXES (kb/Work PB188). The image arm used to hand-spell
        // `StrStore(chars, pic.Length)` — the PICTURE's DIGIT COUNT — which was right only while every
        // StoreAsImage numeric leaf was zoned DISPLAY, where digits and bytes are the same number. Since the
        // V59/PB164 widening a StoreAsImage leaf can be BYTE-FORM, and then they are not: `PIC 9(4) COMP` is 4
        // digits and 2 bytes, `PIC 9(18) COMP-5` is 18 and 8, `PIC S9(3) COMP-3` is 3 and 2 — and even a zoned
        // `PIC S999 SIGN SEPARATE` is 3 and 4. The window a CharImage leaf occupies is item.ImageWidth
        // (StorageFormPass), so a pic.Length-wide seed displaced every following member of the group image.
        // Routing through NumFormatImage — the SAME encoder the numeric arm below and GroupImageCodec use —
        // takes the width from the item's own pinned byte form BY CONSTRUCTION, and makes the leniency MEAN the
        // same thing on both axes: the number, encoded as this item stores numbers.
        if (effRaw is { } q && q.StartsWith('"') && pic.Category is PicCategory.Numeric && !pic.IsFloat)
        {
            string unscaledText = EmitText.UnscaledAtScale(CobolLiteral.Decode(q), pic.Scale);
            return item.StoreAsImage
                ? RuntimeApi.NumFormatImage(unscaledText, item.ProfileName)
                : CarrierInit(unscaledText, pic);
        }

        // A numeric leaf stored as its character image (whole-group-aliased / Tier-B): initialize to the BYTES
        // of its unscaled VALUE (zoned digits for DISPLAY, radix-2 / BCD for BINARY / PACKED — V59) (a numeric/figurative VALUE → that value; no VALUE → 0). The _P_ profile is
        // declared textually earlier (EmitProfiles runs first), so it is initialized before this use.
        if (item.StoreAsImage)
        {
            // A WINDOWED FLOAT member (the Step D arm-1 dissolution) seeds its IEEE window bytes from the
            // (float-literal or zero) VALUE through the ONE literal recipe (RawValueAsFloat) — the integer
            // lane would throw NoByteImage on an Ieee profile.
            // No VALUE: §14.6.2.3.2 action 1's background first (owner decision R53, kb/Work PB1134).
            if (effRaw is null && NoValueSeed(item, pic, recipe) is { } background) return background;
            if (pic.IsFloat)
                return RuntimeApi.NumFormatImageFloat(
                    effRaw is { } fv && FigurativeInitializer(fv, pic) is null ? RawValueAsFloat(fv, pic) : "0d",
                    item.ProfileName, pic.IsSingle);
            string unscaled = effRaw is { } rv && FigurativeInitializer(rv, pic) is null
                ? EmitText.UnscaledAtScale(rv, pic.Scale)
                : "0L";
            return RuntimeApi.NumFormatImage(unscaled, item.ProfileName);
        }

        // ⛔ THE NO-VALUE BRANCH — §14.6.2.3.2 action 1, the BACKGROUND. `pic.DefaultInitializer` is the
        // NO-CLAUSE baseline and stays exactly that; when the OPTIONS paragraph wrote an INITIALIZE clause whose
        // section list selects this item, the background is the specified-fill-character instead. Action 1
        // precedes action 2 (the VALUE seed) and this is the only place that ordering has to appear, because a
        // field initializer is one expression and every VALUE-bearing path above has already returned.
        // ONE choke point, shared with the Tier-B image arm — see InitialStateBackground.
        if (effRaw is not { } raw) return NoValueSeed(item, pic, recipe) ?? pic.DefaultInitializer;

        // A FORMAT-2 (LOCALE) item's numeric VALUE has NO compile-time image — §13.18.40.5 r11 + §14.6.6 r6 make
        // the locale the one current AT THE TIME of editing — so the initializer is a RUNTIME CobolLocaleEdit
        // call (the ONE producer, RuntimeApi.LocaleEditCompose; PB64 T6). A quoted literal falls through to the
        // verbatim store below (§13.18.63.3 SR7 — the programmer supplies the edited form).
        if (pic.LocaleEdit is not null && !raw.StartsWith('"') && !raw.StartsWith('\'')
            && TryParseNumeric(FigurativeKind(raw) == 'Z' ? "0" : raw, out var luv, out int lsc)
            && (FigurativeKind(raw) != 'Z' || ctx.Data.Edition.DialectLevel >= 2023))
            return RuntimeApi.LocaleEditCompose(pic, luv, lsc, item.BlankWhenZero);

        // A NUMERIC-EDITED item's numeric VALUE (a numeric literal, or the figurative ZERO at >= 2023) is its EDITED
        // image — the ONE compose (EditedImageOfNumericValue) the level-88 membership test shares. Below 2023 a
        // figurative ZERO falls through to the FigurativeInitializer zero-fill (the pre-2023 behavior, VCR 35).
        if (pic.Category is PicCategory.NumericEdited
            && EditedImageOfNumericValue(ctx.Data.Edition.DialectLevel,
                    item, pic, raw) is { } editedImage)
            return EmitText.CsLiteral(editedImage);

        // ⛔ A BOOLEAN ITEM'S VALUE IS ONE QUESTION WITH ONE ANSWER (kb/Work PB584): its declared boolean
        // POSITIONS. Three lanes used to answer it — this one, the bit-run carrier
        // (<c>GroupImageCodec.OneBitCarrierOf</c>) and the character-image seed — and they disagreed, each in
        // its own direction. It is asked here so the arms below cannot be extended for one lane only.
        if (pic.Category is PicCategory.Boolean) return BooleanCarrierOf(raw, pic);

        // Figurative constants (ZERO / SPACE / HIGH-VALUE / LOW-VALUE / QUOTE / NULL) fill the item to its width.
        if (FigurativeInitializer(raw, pic) is { } fig) return fig;

        // ALL "literal" (§8.3.3.6.2 Format 6, where ALL is REQUIRED): the literal repeated to the item width
        // (ISO §8.3.3.6.4 GR2; §8.3.3.6.3 SR3 forbids a multi-character literal-1 on a numeric item). Read through
        // THE one operand classifier, so this arm and the SET store see the same spellings (kb/Work PB461).
        if (FigurativeConstants.Classify(raw).AllLiteral is { } allLit && pic.Category is not PicCategory.Numeric)
            return EmitText.CsLiteral(EmitText.RepeatToWidth(CobolLiteral.Decode(allLit), pic.Length));

        return pic.Category switch
        {
            // A numeric-edited item's NUMERIC VALUE was composed above (EditedImageOfNumericValue); an alphanumeric
            // literal stores verbatim (§13.18.63.3 SR7 / NOTE 3: the programmer supplies the edited form).
            // National VALUE stores like alphanumeric on the char substrate (§13.18.63 SR5 — the N"…" literal,
            // already prefix-stripped by DecodeCobolString). The BOOLEAN arm returned above, through
            // <see cref="BooleanCarrierOf"/>.
            PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}"),
            PicCategory.Numeric when pic.IsFloat => RawValueAsFloat(raw, pic),
            PicCategory.Numeric => CarrierInit(EmitText.UnscaledAtScale(raw, pic.Scale), pic),
            _ => pic.DefaultInitializer,
        };
    }

    /// <summary>Cast a numeric VALUE literal to the item's CARRIER type where C# has no implicit conversion: an
    /// unsigned BinaryCapacity item carries <c>ulong</c> / <c>UInt128</c> (kb/Work R10), and its VALUE literal —
    /// a non-negative source literal of ≤31 digits (ISO §8.3.3.3.2), rendered as <c>…L</c> or <c>Int128.Parse</c> —
    /// converts exactly.</summary>
    private static string CarrierInit(string literal, PicInfo pic) =>
        pic.IsUnsignedWideBinary ? $"(UInt128)({literal})"
        : pic.IsUnsignedLongBinary ? $"(ulong)({literal})"
        : literal;

    /// <summary>The compile-time EDITED IMAGE of a numeric-edited item's numeric VALUE — a numeric literal converted
    /// "according to the rules for the MOVE statement" (ISO §13.18.63.3 SR6; formats 1, 2 and 4, so the item VALUE
    /// and a level-88 condition-name value alike), or the figurative ZERO / ZEROES (with or without ALL) at
    /// >= 2023, where SR6 treats it identically to the literal zero (VCR 35 — Annex E.2 item 28: pre-2023 it was
    /// a left-justified zero-fill, which the caller keeps). Null when <paramref name="raw"/> is anything else (an
    /// alphanumeric literal is SR7's edited form as written; SPACE / HIGH-VALUE … are the figurative fill). The
    /// dispatch on the item's FORM lives HERE and nowhere else (D21/PB66): a floating-point numeric-edited item
    /// composes through <see cref="RuntimeApi.EditComposeFloat"/> (the floating-point literal, or a zero form), a
    /// fixed-point one through <see cref="RuntimeApi.EditCompose"/> — the SAME runtime the MOVE uses, so the baked
    /// image is what MOVE literal TO item would store (BLANK WHEN ZERO included, NOTE 2).</summary>
    /// <param name="dialectLevel">The compilation's edition (<c>EditionContext.DialectLevel</c>).</param>
    /// <param name="decimalPointIsComma">ISO §12.3.7 GR14a's DECIMAL-POINT IS COMMA.</param>
    /// <remarks>⛔ THE PARAMETERS ARE THE TWO FACTS THE RULE NEEDS, NOT AN <c>EmitContext</c>, and that is
    /// deliberate (kb/Work PB920). §13.18.63.3 SR6 is not only a code-generation rule: SR26 and SR27 compare
    /// "<i>the value of</i>" two VALUE-clause operands, and on a numeric-edited subject that value IS this
    /// edited image — so <c>DataBinder</c> has to ask the same question during BINDING, where no
    /// <c>EmitContext</c> exists. An emit-context parameter would have forced a second copy of SR6 into the
    /// binder, which is the defect this method was extracted to end
    /// (<c>ConditionValueRecipeDriftTests</c> names every reader).</remarks>
    internal static string? EditedImageOfNumericValue(int dialectLevel, DataItem item, PicInfo pic, string raw)
    {
        bool decimalPointIsComma = pic.DecimalPointIsComma;   // the DESCRIBING unit's mode (§12.3.7.4 GR14 b; kb/Work PB2554)
        // A format-2 (LOCALE) item has NO compile-time image (the locale is runtime data) — the callers carry
        // their own runtime arm (RuntimeApi.LocaleEditCompose); returning null here keeps the EditMask derefs
        // below unreachable for it (PB64 T6).
        if (pic.LocaleEdit is not null) return null;
        if (raw.StartsWith('"') || raw.StartsWith('\'')) return null;
        bool zeroFigurative = FigurativeKind(raw) == 'Z';
        if (zeroFigurative && dialectLevel < 2023) return null;
        if (pic.IsFloatEdited)
            return zeroFigurative || TryParseFloatLiteral(raw, out _, out _)
                ? RuntimeApi.EditComposeFloat(zeroFigurative ? Int128.Zero : ParsedSig(raw), zeroFigurative ? 0 : ParsedExp(raw),
                    pic.EditMask!, item.BlankWhenZero, decimalPointIsComma, pic.EditingRules)
                : null;
        if (zeroFigurative)
            return RuntimeApi.EditCompose(Int128.Zero, pic.Scale, pic.EditMask!, item.BlankWhenZero,
                pic.CurrencyString, decimalPointIsComma, pic.EditingRules);
        return TryParseNumeric(raw, out var uv, out int sc)
            ? RuntimeApi.EditCompose(uv, sc, pic.EditMask!, item.BlankWhenZero, pic.CurrencyString,
                decimalPointIsComma, pic.EditingRules)
            : null;

        static Int128 ParsedSig(string r) { TryParseFloatLiteral(r, out var s, out _); return s; }
        static int ParsedExp(string r) { TryParseFloatLiteral(r, out _, out int e); return e; }
    }

    /// <summary>Parse a numeric literal — the FLOATING-POINT form too (ISO §8.3.3.3.3: two fixed-point literals joined by
    /// E, e.g. <c>-1.5E+3</c>) — to a significand and a power of ten (kb/Work PB66); a fixed-point literal is its unscaled
    /// value at −scale, the figurative ZERO / a zero literal 0E0.</summary>
    private static bool TryParseFloatLiteral(string raw, out Int128 sig, out int exp10)
    {
        sig = 0; exp10 = 0;
        string t = raw.Trim().ToUpperInvariant();
        // ⛔ THE §8.3.3.6.2 VOCABULARY IS ASKED, NEVER RESPELLED (kb/Work PB461, PB933 — this was a private copy
        // of the Format-1 word list inside the very recipe that owns the clause). The WORD map is asked rather
        // than Classify because §8.3.3.6.3 SR1a restricts this context to "ZERO (ZEROS, ZEROES) without the ALL
        // phrase", which is exactly the strip Classify performs and this site must not.
        if (FigurativeConstants.KindOf(t) is 'Z') return true;
        return CobolNet.Common.NumericLiteral.TryParseExact(t, out sig, out exp10);   // the ONE exact parser (PB99)
    }

    /// <summary>Parse a canonical (dot-decimal) numeric VALUE text to its unscaled value + scale, for
    /// compile-time editing. False for any non-numeric shape (the caller falls back to verbatim store).</summary>
    internal static bool TryParseNumeric(string text, out Int128 unscaled, out int scale)
    {
        unscaled = 0;
        scale = 0;
        string t = text.Trim();
        bool neg = t.StartsWith('-');
        if (neg || t.StartsWith('+')) t = t[1..];
        int dot = t.IndexOf('.');
        string digits = dot < 0 ? t : t.Remove(dot, 1);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return false;
        scale = dot < 0 ? 0 : t.Length - dot - 1;
        foreach (char c in digits) unscaled = unscaled * 10 + (c - '0');
        if (neg) unscaled = -unscaled;
        return true;
    }

    /// <summary>An ORDINARY (alphanumeric) group's as-if elementary description, taken from the VALUE clause's
    /// OWN rule for that subject — ISO §13.18.63.3 SR4: "If the item is of category alphabetic, alphanumeric,
    /// or alphanumeric-edited literals in the VALUE clause shall be alphanumeric literals. … Alphanumeric
    /// literals in the VALUE clause of an alphanumeric group item shall not exceed the size of the group item."
    /// That one sentence gives BOTH halves this needs: the literal's category is alphanumeric, and the bound is
    /// the SIZE OF THE GROUP ITEM, which is <see cref="DataItem.ImageWidth"/>.
    /// <para>⚠ NOT §8.8.4.2.1. That clause does say an alphanumeric group item is "treated as an elementary
    /// alphanumeric data item", but its sentence opens "For comparison" — it is the COMPARISON rule, and a
    /// VALUE store is not a comparison. It was inherited from the SET emitter's comment when the private
    /// recipe there was deleted (kb/Work PB560), and re-deriving it is what caught that.</para>
    /// <para>The bit / national group case is NOT here: those carry a real as-if PICTURE (§13.18.29.4 GR1b/GR2b)
    /// that <see cref="DataItem.OperandPic"/> already answers, and duplicating it would be a second width
    /// rule.</para></summary>
    private static PicInfo AsIfAlphanumericGroup(DataItem item) =>
        new(PicCategory.Alphanumeric, Usage.Display, item.ImageWidth, Digits: 0, Scale: 0, Signed: false);

    /// <summary>If <paramref name="raw"/> is a figurative constant, its C# initializer given the receiver's category
    /// and width; otherwise null (ISO §8.3.3.6; native HIGH/LOW = U+FFFF/U+0000, owner R52).</summary>
    /// <summary>⛔ THE ONE INITIAL BOOLEAN CARRIER of an elementary boolean item with a VALUE clause — its
    /// <c>pic.Length</c> boolean positions, in the SAME three arms the standard writes them in, for every lane
    /// that needs them (the record-struct field, the §8.5.1.6.3 bit-run carrier, and the character-image seed —
    /// kb/Work PB584).
    /// <list type="bullet">
    /// <item>A FIGURATIVE constant fills the positions: ISO §8.3.3.6.4 GR4 makes the zero format "one or more of
    /// the boolean character '0'", and GR2 repeats the string "until the size of the resultant string is greater
    /// than or equal to the number of character positions in the associated data item".</item>
    /// <item>A Format-6 <c>ALL literal-1</c> (§8.3.3.6.2, where ALL is REQUIRED) is the literal repeated to the
    /// item's positions by that same GR2 — THE arm the bit-carrier lane did not have, so
    /// <c>VALUE ALL B"1"</c> came back all zeros through a REDEFINES alias and all ones without one.</item>
    /// <item>A plain boolean literal zero-pads on the right (§13.18.63.3 SR10; §14.6.8.6).</item>
    /// </list>
    /// The USAGE is NOT this method's business: whether those positions are then PACKED is
    /// <c>GroupImageCodec.BooleanImageOf</c>'s single decision.</summary>
    public string BooleanCarrierOf(string raw, PicInfo pic) =>
        FigurativeInitializer(raw, pic)
        ?? (FigurativeConstants.Classify(raw).AllLiteral is { } allLit
            ? EmitText.CsLiteral(EmitText.RepeatToWidth(CobolLiteral.Decode(allLit), pic.Length))
            : RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}",
                                         justifiedRight: false));

    public string? FigurativeInitializer(string raw, PicInfo pic)
    {
        if (FigurativeKind(raw) is not { } k) return null;
        string fillChar = FigurativeConstants.Fill(k, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating);
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>The figurative KIND of a VALUE text — Formats 1–5, where the word <c>ALL</c> is OPTIONAL
    /// (ISO §8.3.3.6.2) — or null when it is not one of those (a Format-6 <c>ALL literal-1</c> is the literal
    /// path below). The ONE detector shared by <see cref="FigurativeInitializer"/>, the VCR 35 numeric-edited
    /// figurative-ZERO branch, and the §13.18.63.4 GR5 group-area rule
    /// (<see cref="GroupValueSlicer.AreaTextOf"/> — internal for that third rider, kb/Work PB184); it reads the
    /// operand through <see cref="FigurativeConstants.Classify"/>, THE one classifier the SET store and the
    /// condition test read it through as well (kb/Work PB461).</summary>
    internal static char? FigurativeKind(string raw) => FigurativeConstants.Classify(raw).Kind;

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item. Internal:
    /// the ONE literal recipe — the group-image codec's float backing seed reuses it (Step D).</summary>
    internal static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.IsSingle ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";   // COMP-1/FLOAT-SHORT → float literal, else double

    /// <summary>Whether <paramref name="item"/> belongs to a FILE SECTION record (its root is some FD / SD record) —
    /// the section fact §13.4.4 GR1 keys on. File-section records emit as unit fields through the same initial-state
    /// path as WORKING-STORAGE, so the section has to be asked of the ROOT (kb/Work PB1241).</summary>
    private bool InFileSection(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return ctx.Data.Files.Any(f => f.Records.Contains(root));
    }
}
