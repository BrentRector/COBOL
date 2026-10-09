// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The memoized physical-field model of the DATA DIVISION (P7 Step 9l — the 5th concern of the
/// former FieldEmitter, given its own home per the phase doc): which C# fields a group/root forest actually
/// emits (REDEFINES views collapse to their class backing) and each field's image-width contribution. The
/// memoization turns the otherwise-exponential nested-group emission into linear time (the ~49-level CCVS
/// nesting). Wired by <see cref="DataEmitter"/> (Values/Codec are the composed-initializer back-edges).</summary>
internal sealed class PhysicalModel(EmitContext ctx)
{
    /// <summary>Memoized physical-field list per group item — the cache that turns the otherwise-exponential
    /// nested-group emission (width and init recursively recomputing each other) into linear time. The root forest
    /// is cached separately in <see cref="_rootPhysCache"/>.</summary>
    private readonly Dictionary<(DataItem Owner, Subscripts Subs, SeedRecipe Recipe, object? Options, object? Activation), IReadOnlyList<Physical>> _physCache = new(new PhysKeyComparer());
    private IReadOnlyList<Physical>? _rootPhysCache;

    /// <summary>The memo key's equality: every reference member by IDENTITY (the OPTIONS model is a value-equal record, and
    /// hashing it on every group lookup would put its structural hash on the emitter's hottest path), the occurrence
    /// context and the recipe by value.</summary>
    private sealed class PhysKeyComparer
        : IEqualityComparer<(DataItem Owner, Subscripts Subs, SeedRecipe Recipe, object? Options, object? Activation)>
    {
        public bool Equals((DataItem Owner, Subscripts Subs, SeedRecipe Recipe, object? Options, object? Activation) a,
                           (DataItem Owner, Subscripts Subs, SeedRecipe Recipe, object? Options, object? Activation) b) =>
            ReferenceEquals(a.Owner, b.Owner) && a.Subs.Equals(b.Subs) && a.Recipe == b.Recipe
            && ReferenceEquals(a.Options, b.Options) && ReferenceEquals(a.Activation, b.Activation);

        public int GetHashCode((DataItem Owner, Subscripts Subs, SeedRecipe Recipe, object? Options, object? Activation) k) =>
            HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(k.Owner), k.Subs, k.Recipe,
                k.Options is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(k.Options),
                k.Activation is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(k.Activation));
    }

    /// <summary>The composed-initializer back-edges (set once by <see cref="DataEmitter"/> — the physical
    /// model, the VALUE initializers, and the image codec are mutually recursive by design: a field's Init is
    /// a VALUE initializer, a Tier-B backing's seed is an image init, and both walk the physical model).</summary>
    public ValueInitializer Values { get; set; } = null!;
    public GroupImageCodec Codec { get; set; } = null!;

    /// <summary>A field that physically appears in the emitted C# — an item's own field, OR a REDEFINES class's single
    /// string backing (which replaces ALL the class's members). A REDEFINES <i>view</i> yields no physical field
    /// (ISO §13.18.44; COBOLNET_DESIGN §4.1 — never two stored fields per storage area).</summary>
    /// <summary>One emitted struct field. <paramref name="Width"/> is the field's TOTAL contribution to its group's
    /// character image — element-image-width × <paramref name="Occurs"/> for a fixed-OCCURS table, else the item's own
    /// image width. <paramref name="Occurs"/> is the fixed occurrence count (0 = not a table), so the image facility
    /// knows to concat/distribute across the array's elements. <paramref name="NumLeaf"/> is the source leaf when the
    /// field stores a NATIVE fixed-point numeric (<c>long</c>/<c>Int128</c>; DISPLAY, BINARY, or PACKED usage) — the
    /// image facility then encodes/decodes it through <c>CobolNum.FormatDisplay</c>/<c>ParseDisplay</c> with the
    /// leaf's IMAGE profile (<see cref="ImageProfileOf"/>); null for every string-shaped field (alphanumeric, edited,
    /// <see cref="DataItem.StoreAsImage"/>, a Tier-B class backing) and for nested group structs.</summary>
    /// <param name="BitRun">⛔ Set ONLY on the FIRST field of a <c>USAGE BIT</c> run, and it is what lets a run
    /// span several FIELDS while occupying ONE image slice (design D19, fix-queue PB43). §8.5.1.6.3 puts two
    /// same-level bit items at successive BIT positions — they SHARE a byte — so their image cannot be composed
    /// field-by-field the way every other member is. The run's leaves are listed here, the first field carries the
    /// whole run's <see cref="Width"/> (<c>ceil(bits/8)</c>) and emits one <c>CobolBits.Pack</c> over the
    /// concatenated carriers, and the remaining fields carry <c>Width = 0</c> and contribute nothing — so the
    /// group's image width still falls out of a plain sum and no caller needs to know runs exist. Null on every
    /// non-bit field and on the continuation fields of a run.</param>
    /// <param name="NatLeaf">⛔ Set on a NATIONAL leaf, and it is what makes the field's slice of the group
    /// image its UTF-16BE BYTES rather than its carrier characters (kb/Work PB327; ISO §13.18.60.4 GR8 leaves the
    /// size of a national character to the implementor and D-N1 pins two bytes, high-order first). The predicate is
    /// <see cref="NationalWindow.PositionsOf"/> — THE ONE national test, shared with the byte-window carriage gate
    /// (<c>DataBinder.ByteWindowResidueOf</c>) and with the Tier-B window geometry, so the codec cannot disagree
    /// with them about which leaves are national. ⛔ It COMPOSES with <paramref name="NumLeaf"/> rather than
    /// excluding it (design D-N7, kb/Work PB646): this flag decides how a CHARACTER is serialized, NumLeaf decides
    /// how the CARRIER becomes characters, and §13.18.60.3 SR12's national-form numeric is both at once — a
    /// native fixed-point carrier whose zoned digit run is written in national characters. Null on every
    /// non-national field.</param>
    /// <param name="SlotLeaf">Set on a class pointer / class object leaf (<see cref="SlotWindow.CarriedBySlot"/>; kb/Work
    /// PB244): the item whose slice of the one-way transfer image <c>GroupImageCodec.AsImageOf</c> renders — the
    /// pointer's storage image (<c>PointerImage</c>, DOC-A.1-216; kb/Work PB1071) when
    /// <see cref="SlotWindow.CarriesPointerImage"/>, else the reserved placeholder positions. Null on every other field.</param>
    internal readonly record struct Physical(string Name, string Type, int Width, bool IsGroupStruct, string Init, string Comment, int Occurs = 0, DataItem? NumLeaf = null, IReadOnlyList<DataItem>? BitRun = null, DataItem? NatLeaf = null, DataItem? SlotLeaf = null);

    /// <summary>The memoized physical fields of a group's children (the root forest under the sentinel).
    /// <para>⚠ <paramref name="subs"/> is the OCCURRENCE CONTEXT (see <see cref="ValueInitializer.FieldInit"/>) and
    /// it is the ONLY input <see cref="Physical.Init"/> varies on; every other member of a <see cref="Physical"/> —
    /// name, type, width, the leaf classifications — is a property of the DECLARATION alone. So the memo stays,
    /// and is bypassed only along the spine of a group that actually contains a Format-2 (table) VALUE
    /// (<see cref="DataItem.ContainsTableValue"/>), where two occurrences of the same group genuinely compose
    /// different initializers. Everywhere else the cache answers, which is what keeps the ~49-level CCVS nesting
    /// linear (width and init otherwise recompute each other exponentially).</para></summary>
    /// <param name="recipe">The seed recipe the members' <see cref="Physical.Init"/> compose by — the OTHER input
    /// Init varies on (kb/Work PB1267), so it is part of the memo key; everything else in a Physical is still the
    /// declaration's alone. ⛔ A THIRD INPUT is the INITIAL-STATE CONTEXT (kb/Work PB1134): the OPTIONS clause in force
    /// (<c>Data.Options</c>) and the LOCAL-STORAGE roots of the activation being emitted (<c>ActivationLocalRoots</c>)
    /// decide what a VALUE-less item is filled with (<c>InitialStateBackground</c>). <c>OoEmitter</c> swaps both for an OO
    /// method's LOCAL-STORAGE, so the key carries them: without that a method-local group's members were answered from the
    /// entry the CLASS-level emission cached first, with no background, and a group under a method's
    /// <c>INITIALIZE LOCAL-STORAGE</c> clause kept its zeros while the method's elementary roots took the fill.</param>
    public IReadOnlyList<Physical> PhysicalChildrenOf(DataItem owner, Subscripts subs = default,
        SeedRecipe recipe = SeedRecipe.InitialState)
    {
        // ⛔ THE MEMO SURVIVES THE OCCURRENCE CONTEXT, and it has to: width and Init recompute each other, so
        // dropping the cache on a table-VALUE spine restores exactly the exponential nested-group emission the
        // cache exists to kill. The key carries the context ONLY where the subtree's initializers actually vary
        // with it — one entry per group everywhere else, one per (group, occurrence tuple) on the spine, which
        // is proportional to the source the emitter writes for that table anyway. The recipe adds at most one
        // entry per group inside an INITIALIZED dynamic-capacity table's element.
        var key = (owner, owner.ContainsTableValue ? subs : default, recipe, (object?)ctx.Data.Options, (object?)ctx.ActivationLocalRoots);
        if (_physCache.TryGetValue(key, out var cached)) return cached;
        ListBuilds++;
        var list = BuildPhysicals(owner.Children, subs, recipe).ToList();
        _physCache[key] = list;
        return list;
    }

    /// <summary>The memoized physical fields of the top-level (01/77) forest.</summary>
    public IReadOnlyList<Physical> RootPhysicals()
    {
        if (_rootPhysCache is null) { ListBuilds++; _rootPhysCache = BuildPhysicals(ctx.Data.Roots, default, SeedRecipe.InitialState).ToList(); }
        return _rootPhysCache;
    }

    /// <summary>How many physical-field lists this model has BUILT — the work the memo bounds. With the memo it is one
    /// per group (plus one per occurrence tuple on a table-VALUE spine) plus one for the root forest, i.e. linear in the
    /// DATA DIVISION; the DEVLOG 502 regression (width and init recomputing each other) roughly doubles it per nesting
    /// level. A deterministic measure of the growth rate, read by <c>DeepNestingTests</c> (kb/Work PB1590) instead of a
    /// wall-clock ceiling that a loaded CI runner turned red twice.</summary>
    internal int ListBuilds { get; private set; }

    /// <summary>Test seam (kb/Work PB1590): when set, receives each model a <see cref="DataEmitter"/> creates. An
    /// <see cref="AsyncLocal{T}"/>, so only the test's own compilation reports to it — the Unit assembly compiles in
    /// parallel, and a process-global hook would hear every other test's models.</summary>
    internal static readonly AsyncLocal<Action<PhysicalModel>?> Observer = new();

    /// <summary>The physical fields a run of sibling items emits: skip REDEFINES views; substitute a Tier-B class's ONE
    /// string backing (emitted once, at the canonical) for the whole class; a Tier-A view forwards to its canonical's
    /// field. The class's numeric <c>NumProfile</c>s are still emitted elsewhere (EmitProfiles — D9).</summary>
    private IEnumerable<Physical> BuildPhysicals(IEnumerable<DataItem> items, Subscripts subs, SeedRecipe recipe)
    {
        // D19/PB43 — the §8.5.1.6.3 bit RUNS in this sibling list, keyed by the leaf that starts each one. A run is
        // a maximal stretch of consecutive USAGE BIT leaves at the SAME level: exactly the items the standard puts
        // at successive bit positions, i.e. the ones that share bytes. Computed once so the emit loop below can
        // stay a straight per-child walk — and computed by BitLayout, because the compile-time image SEED needs
        // the same answer and used to have none (kb/Work PB584).
        var siblings = items as IList<DataItem> ?? items.ToList();
        var runs = BitLayout.RunsOf(siblings);

        foreach (var c in siblings)
        {
            if (!(c.IsGroup || c.IsElementary)) continue;
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                // The whole redefines class is ONE string backing (the canonical's VALUE seeds it, SR9); every member
                // is a window over it. A non-canonical Tier-B member yields no field.
                if (c.IsCanonical)
                    yield return new Physical(cls.BackingCsName, "string", cls.Width, false,
                        RuntimeApi.StrStore(Codec.ImageInitOf(c, subs: subs, recipe: recipe), $"{cls.Width}"), $"REDEFINES backing for {c.CobolName}");
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } && !c.IsCanonical)
                continue;   // a Tier-A view forwards to the canonical's stored field
            string comment = c.CobolName is { } n ? $"{n}{(c.Occurs is { } o ? $" OCCURS {o}" : "")}" : "FILLER";
            // For a group child, use its PHYSICAL image width (skips its own redefines views, counts a contained
            // backing once) — the raw DataItem.ImageWidth over-counts a group that contains a redefines class. A fixed
            // OCCURS table contributes its per-occurrence image width × the count to the group image (ISO §14.9).
            // ⛔ BYTES, NOT CARRIER POSITIONS (kb/Work PB327): AsImage()/FromImage() is the STORAGE face of a
            // group — a numeric leaf contributes its radix-2 / BCD / IEEE bytes, a USAGE BIT run its packed bytes —
            // and a NATIONAL position occupies TWO of those bytes (ISO §13.18.60.4 GR8 leaves the size to the
            // implementor; D-N1 pins two, UTF-16BE). ByteWidth IS ImageWidth for every other leaf kind, so this is
            // byte-identical for national-free programs. The CARRIER face of a national group is AsNat()
            // (§13.18.29.4 GR2b), exactly as a bit group's carrier face is AsBits().
            int elemWidth = c.IsGroup ? PhysicalImageWidth(c) : c.ByteWidth;
            int occurs = c.Occurs ?? 0;
            int width = occurs > 0 ? elemWidth * occurs : elemWidth;
            // A NATIVE fixed-point numeric field (the ElementType test excludes the string-stored shapes —
            // StoreAsImage leaves, edited items — and floats): its slice of the group image is its BYTE FORM
            // (zoned digits, radix-2, BCD — PicInfo.HasImageByteForm, THE ONE image predicate; kb/Work PB164
            // admitted COMP-5/BINARY-CHAR..DOUBLE, whose drifted exclusion here was the second copy of the
            // union), encoded/decoded through CobolNum (COBOLNET_DESIGN §14.4). INDEX qualifies since the R40
            // pin (ByteForm Binary, 8 bytes; see DataItem.IsImageCapable).
            // ⛔ DERIVED from the predicate alone (kb/Work PB164 — the review fleet caught the carrier-name
            // list `"long" or "Int128"` surviving as a FIFTH drifted copy: an unsigned wide COMP-5/
            // BINARY-DOUBLE leaf carries ulong/UInt128, got NumLeaf null, and the codec emitted
            // string-member code over a ulong field — uncompilable generated C#). StoreAsImage leaves are
            // string-stored and take the pass-through arm.
            // ⛔ A NATIONAL LEAF'S SLICE IS ITS BYTES (kb/Work PB327) — THE ONE national test, so the codec, the
            // byte-window gate and the Tier-B window geometry share one answer about which leaves are national.
            // ⛔ THE TWO ARE ORTHOGONAL, AND COMPOSE (design D-N7, kb/Work PB646): natLeaf says how a
            // CHARACTER is serialized (UTF-16BE pairs), numLeaf says how the CARRIER becomes characters (the
            // zoned/radix-2/BCD/IEEE image). §13.18.60.3 SR12's national-form NUMERIC is both, and asking
            // numLeaf only where natLeaf was null made them mutually exclusive — sound only while that shape
            // was staged loud and unreachable.
            DataItem? natLeaf = !c.IsGroup && NationalWindow.PositionsOf(c) is not null ? c : null;
            DataItem? numLeaf = !c.IsGroup && !c.StoreAsImage && c.Pic is { HasImageByteForm: true } ? c : null;
            // D19/PB43 — a USAGE BIT leaf's image is the PACKED run it belongs to, not its own carrier. The run's
            // leader carries the whole run's byte width; a continuation carries 0, so the group's image width is
            // still a plain sum of Width and every downstream caller is unchanged.
            if (runs.IsInRun(c))
            {
                var run = runs.RunLedBy(c);
                // A bit GROUP member keeps its record-struct type (IsGroupStruct) — its own AsImage/FromImage still exist
                // for the standalone (record) case — but inside the run its slice is the run's (D20/PB79).
                yield return new Physical(c.CsName, c.FieldType, run is null ? 0 : BitLayout.RunCharacters(run),
                    c.IsGroup, Values.FieldInit(c, subs, recipe), comment, occurs, null, run);
                continue;
            }
            // A class pointer/object leaf (kb/Work PB244): its slice of the one-way transfer image is its pointer image
            // (PB1071) or its reserved placeholder positions (D-SLOT) - GroupImageCodec.SlotImageOf, the same recipe
            // the storage seed uses.
            yield return new Physical(c.CsName, c.FieldType, width, c.IsGroup, Values.FieldInit(c, subs, recipe), comment, occurs, numLeaf, null, natLeaf,
                SlotWindow.CarriedBySlot(c) ? c : null);
        }
    }

    /// <summary>The emitted character-image width of an item: a leaf's own width; a group's = the sum of its physical
    /// fields (a contained REDEFINES class contributes its single backing width once, its views nothing).</summary>
    private int PhysicalImageWidth(DataItem item) =>
        item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ByteWidth;

}
