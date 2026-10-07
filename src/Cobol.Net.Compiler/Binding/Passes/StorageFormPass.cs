// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Passes;

/// <summary>
/// The LAST data-model pass (rearchitecture PHASE 05 §2.5 step 10 / PHASE-06 Step 3): the manifest-ordered OWNER of
/// the storage-form decision. <see cref="Run"/> computes the canonical <see cref="StorageForm"/> for every
/// elementary item, ONCE, from the COLLECTED image facts (P5.7 — the mutable <c>StoreAsImage</c> flag and its 9
/// cross-layer write sites are DELETED; <see cref="DataItem.StoreAsImage"/> is a read-only projection of the
/// Storage computed here). Bind-time rules RECORD facts instead of mutating storage state:
/// <see cref="DataBinder.ImageForcedItems"/> carries the Tier-B/CALL-cell/file-record/print-item/figurative/ref-mod
/// records at the same instants the legacy writes fired, and the bind-time <c>NumericImagePlace</c> wrap decision
/// reads it with identical mid-bind timing (<c>DataBinder.IsImageBackedEarly</c>).
/// <para><b>Prove-then-delete lineage:</b> D0 classified FROM the final flag and <see cref="Verify"/> proved the
/// derived facts equal corpus-wide; P5.7a re-derived the promotion from the collected facts IN PARALLEL with the
/// flag and the same corpus gate proved the two equal (identity #1's one real run); P5.7b then deleted the flag
/// writes (DEVLOG 777).</para>
/// </summary>
internal static class StorageFormPass
{
    /// <summary>The GROUP pass body (P6 Step 3 / P5 Step 7 — <c>BindPipeline.GroupTail</c>, Requires
    /// <c>UsageCollected</c>, Produces <c>StorageComputed</c>): compute <see cref="DataItem.Storage"/> from the
    /// collected facts, then settle the OO crossing forms at the Storage level.</summary>
    public static void Run(GroupBindContext ctx)
    {
        // 1) The promoted-leaf union from the COLLECTED facts, in the legacy write order (P5.7 — the mutable
        //    flag is GONE; StoreAsImage is a read-only projection of the Storage computed here).
        var promoted = ComputePromotedSet(ctx);

        // 2) Classify: the canonical StorageForm from (Pic, tier, dynamic) + the promoted union — over the group
        //    forests AND the interface prototype forests (both sides of an implements pair need Storage before
        //    the storage-level harmonize below).
        foreach (var d in ctx.AllBindersAndInterfaces())
            Compute(d, promoted);

        // 3) The STORAGE-level harmonize: the override/implements crossing-form fixed point, expressed as
        //    NativeInt→CharImage Storage flips (proven equal to the legacy flag-level harmonize by the P5.7a
        //    corpus parity gate before the flag was deleted).
        HarmonizeStorageCrossings(ctx.Session.OoClasses);
    }

    /// <summary>The P5.7 promoted-leaf union — every elementary item whose storage is its CHARACTER IMAGE,
    /// derived from the COLLECTED facts in the exact legacy write order: (1) the bind-time
    /// <see cref="DataBinder.ImageForcedItems"/> records (Tier-B/CALL-cell/file-record/print-item at resolve,
    /// figurative-fill + ref-mod-store at procedure bind); (2) the compiler-temp re-sync (a temp mirrors its
    /// model's PRE-whole-group state — the fused pipeline's re-sync ordering); (3) the whole-group promotion
    /// (§14.9.25.4 MOVE GR4) over <see cref="DataBinder.WholeGroupReferenced"/>.</summary>
    private static HashSet<DataItem> ComputePromotedSet(GroupBindContext ctx)
    {
        var promoted = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var d in ctx.AllBindersAndInterfaces())
            promoted.UnionWith(d.ImageForcedItems);
        foreach (var d in ctx.AllBindersAndInterfaces())
            foreach (var (temp, model) in d.CompilerTempClones)
                if (promoted.Contains(model))
                    promoted.Add(temp);
        foreach (var d in ctx.AllBindersAndInterfaces())
            foreach (var group in d.WholeGroupReferenced)
                AddImagePromotableLeaves(group, promoted);
        // (4) THE ELEMENTARY TWIN of (3) (kb/Work PB992): an elementary item a CHARACTER CHANNEL writes — a group
        //     move's receiver, a BY REFERENCE crossing, a CALL RETURNING receiver (UsageCollectionPass). A native
        //     carrier holds a VALUE, so content with no numeric reading could not survive in it; the leaf
        //     stores its storage image instead, the one representation that holds every content. A REDEFINES
        //     class member keeps its tier's classification (the bind-time image facts' same exclusion).
        foreach (var d in ctx.AllBindersAndInterfaces())
            foreach (var item in d.CharacterChannelItems)
                if (item.Class is null && IsImagePromotable(item))
                    promoted.Add(item);
        // (5) THE §14.6.2.3.2 ACTION-1 BACKGROUND (owner decision kb/Work R53; kb/Work PB1134): an OPTIONS
        //     INITIALIZE clause sets "the storage allocated for the implied or associated sections" to the
        //     specified-fill-character, numeric items included, and a native carrier holds a decoded VALUE, not a
        //     run of fill characters or bytes. So a VALUE-less fixed-point numeric item of a governed section
        //     stores its image, and InitialStateBackground seeds that image with the fill. An item under a VALUE
        //     (its own or an ancestor group's) is excluded: action 2 overwrites the background there.
        foreach (var d in ctx.AllBindersAndInterfaces())
            if (d.Options.Initialize is { HasFill: true } init)
            {
                foreach (var root in d.WorkingStorageRoots)
                    if (init.Governs(root, workingStorageRoot: true, localStorageRoot: false))
                        AddBackgroundFilled(root, promoted);
                foreach (var root in d.LocalStorageRoots)
                    if (init.Governs(root, workingStorageRoot: false, localStorageRoot: true))
                        AddBackgroundFilled(root, promoted);
            }
        // (5b) THE SAME BACKGROUND OVER AN OO METHOD'S LOCAL-STORAGE (kb/Work PB1134, ISO §13.6.4 GR2 → §11.9.10): a
        //      method binds its LOCAL-STORAGE into its OWN roots, which the program's LocalStorageRoots never list,
        //      and it may carry its OWN OPTIONS paragraph, which overrides the class's (§11.9.4 GR1; kb/Work PB1215).
        //      The method's EFFECTIVE clause is the one the emitter seeds with (OoEmitter: MethodOptions, else the
        //      unit's), so the promotion decision and the seed read the same clause.
        foreach (var d in ctx.AllBindersAndInterfaces())
            foreach (var root in d.OoMethodScopedRoots)
                if (root.RootSection == EntrySection.LocalStorage && d.OoRootOwner.TryGetValue(root, out var method)
                    && (method.MethodOptions ?? d.Options).Initialize is { HasFill: true } methodInit
                    && methodInit.Governs(root, workingStorageRoot: false, localStorageRoot: true))
                    AddBackgroundFilled(root, promoted);
        return promoted;

        static void AddBackgroundFilled(DataItem item, HashSet<DataItem> promoted)
        {
            if (item.RawValue is not null || item.ContainsTableValue) return;   // action 2 owns this subtree
            if (!item.IsGroup)
            {
                if (item.Class is null && IsImagePromotable(item)) promoted.Add(item);
                return;
            }
            foreach (var child in item.Children) AddBackgroundFilled(child, promoted);
        }

        static void AddImagePromotableLeaves(DataItem item, HashSet<DataItem> promoted)
        {
            foreach (var child in item.Children)
            {
                // A fixed-OCCURS subordinate is part of the whole-group image too (ISO §14.9 — every OCCURS
                // position); same recursion as the legacy MarkStoreAsImage.
                if (child.IsGroup) AddImagePromotableLeaves(child, promoted);
                else if (IsImagePromotable(child))
                    promoted.Add(child);
            }
        }
    }

    /// <summary>⛔ THE ONE eligibility test for image promotion, shared by the whole-group promotion and the
    /// character-channel promotion (kb/Work PB992): an elementary FIXED-POINT numeric item with a pinned byte form
    /// (<see cref="PicInfo.HasImageByteForm"/>) — usage DISPLAY, BINARY / COMP-5 / BINARY-CHAR…DOUBLE, or PACKED.
    /// <para>⛔ BINARY AND PACKED ARE PROMOTED, NOT DISPLAY ALONE (kb/Work PB1466). Content reaches such a leaf
    /// through a character channel — a group MOVE, a READ into its record, a BY REFERENCE crossing — "without
    /// consideration for the individual elementary or group items" (§14.9.25.4 GR4), so it can be anything, and
    /// ISO §8.8.4.4.4 GR3 n) 1. c. exists to ask about exactly that content: "the content … consists entirely of a
    /// valid representation for the usage and, if a PICTURE clause is specified, the numeric value is within the
    /// range of values implied by the PICTURE clause". A native carrier holds a DECODED value — a packed nibble
    /// A–F or a binary value past the PICTURE's range is normalized away on the way in — so the class condition
    /// was folded to the constant <c>true</c> and the validate-a-packed-field-after-READ idiom answered NUMERIC
    /// over garbage. The storage image holds every content, and the class test reads it
    /// (<c>CobolNum.IsNumericImage</c>, keyed on the byte form). The price is DISPLAY's own: arithmetic on such a
    /// leaf decodes and encodes its image.</para>
    /// <para>⛔ NATIONAL IS PROMOTED TOO (kb/Work PB1466 arm 1, unblocked by kb/Work PB1665). It was excluded because
    /// promotion pins the carrier at the leaf's ImageWidth character POSITIONS — half of a national leaf's storage
    /// (D-N1, two bytes per position) — and a group MOVE was sized by that position count while the group's
    /// AsImage serialized national positions as byte pairs, so a MOVE across a promoted national numeric leaf
    /// truncated the group. A group's ImageWidth is now the storage characters of its image
    /// (<see cref="RecordLayout.ImageWidth"/>), so the carrier of a promoted national numeric leaf (its positions,
    /// the string the codec serializes as UTF-16BE pairs) and the width every group consumer sizes by agree.
    /// USAGE INDEX is class index, never a numeric operand of a class test, and keeps its IndexCell.
    /// Floating-point leaves keep their native carrier, which holds every IEEE bit pattern its usage can
    /// store.</para></summary>
    private static bool IsImagePromotable(DataItem item) =>
        !item.IsGroup && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, HasImageByteForm: true } p
        && p.Usage is not Usage.Index;

    /// <summary>The STORAGE-level twin of <see cref="HarmonizeOverrideCrossings"/> (P5.7): the identical
    /// override-chain + implements-pair fixed point, deciding string-carriage off the just-classified
    /// <see cref="DataItem.Storage"/> and flipping the native side's Storage to <c>CharImage(Numeric)</c>
    /// directly. Runs AFTER <see cref="Compute"/> so interface prototypes carry Storage too.</summary>
    private static void HarmonizeStorageCrossings(OoClassTable classes)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var cls in classes.Classes)
            {
                foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                {
                    if (m.OverrideOf is not { } baseM) continue;
                    changed |= UnifyPair(m, baseM);
                }
                foreach (bool factory in (ReadOnlySpan<bool>)[false, true])
                    foreach (var iface in classes.ImplementsClosure(cls, factory))
                        foreach (var proto in iface.AllPrototypes())
                            if ((factory ? cls.FindFactoryMethod(proto.ExternalizedName) : cls.FindMethod(proto.ExternalizedName)) is { } impl)   // the roster key (PB303)
                                changed |= UnifyPair(impl, proto);
            }
        }

        static bool UnifyPair(OoMethodSymbol a, OoMethodSymbol b)
        {
            bool changed = false;
            for (int i = 0; i < Math.Min(a.Binding!.Formals.Count, b.Binding!.Formals.Count); i++)
                changed |= UnifyCrossing(a.Binding!.Formals[i].Item, b.Binding!.Formals[i].Item);
            if (a.Binding!.Returning is { } r && b.Binding!.Returning is { } br)
                changed |= UnifyCrossing(r, br);
            return changed;
        }

        // Mirrors the legacy UnifyCrossing exactly, at the Storage level: string-carried = group or ANY
        // CharImage form (string categories are CharImage(non-numeric); a promoted numeric is CharImage(Numeric)).
        static bool UnifyCrossing(DataItem a, DataItem b)
        {
            if (StringCarried(a) == StringCarried(b)) return false;
            var native = a.Storage is StorageForm.CharImage { Category: PicCategory.Numeric } ? b : a;
            // ⛔ EVERY BYTE-FORM NUMERIC, not the DISPLAY pair alone (kb/Work PB187). "Only the display-numeric pair
            // can diverge" was true while ComputeTier refused every float / COMP-5 / BINARY-* / INDEX leaf in a
            // REDEFINES / EXTERNAL / BASED class; the PB164 arm-1 dissolution admitted them, so a formal that is
            // REDEFINED in its LINKAGE SECTION (§14.2.2 NOTE 2 — the clause is on the redefiner, never on the
            // formal) is CharImage(Numeric) while its prototype/override twin stays native, and returning false
            // here left the two C# signatures disagreeing (`ref string` against `ref float`). Every
            // HasImageByteForm item HAS a byte image, so the one reconciliation is "promote the native side to
            // the image form". A NATIONAL-form numeric stays out: its carrier positions are half its storage
            // (D-N1), the CARRIAGE question kb/Work PB646 settled.
            if (native.Pic is not { HasImageByteForm: true } || native.Pic.Usage is Usage.National)
                return false;
            native.Storage = new StorageForm.CharImage(native.ImageWidth, PicCategory.Numeric);
            return true;
        }

        static bool StringCarried(DataItem item) =>
            item.IsGroup || item.Storage is StorageForm.CharImage;
    }


    /// <summary>Assign <see cref="DataItem.Storage"/> to every ELEMENTARY item reachable from the binder's roots +
    /// report print items (a group carries no Storage — it emits as a record struct and answers its image facts
    /// recursively over children). <paramref name="promoted"/> is the P5.7 from-scratch promotion union — the
    /// classification never reads the legacy flag.</summary>
    private static void Compute(DataBinder data, HashSet<DataItem> promoted)
    {
        var visited = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var root in data.Roots) Walk(root, visited, promoted);
        // Report print items are synthetic DataItems OFF Roots (they carry image promotions too — a Roots-only
        // walk would miss them, a parity trap).
        foreach (var report in data.Reports)
            foreach (var group in report.Groups)
                foreach (var line in group.Lines)
                    foreach (var field in line.Fields)
                    Walk(field.PrintItem, visited, promoted);
    }

    private static void Walk(DataItem item, HashSet<DataItem> visited, HashSet<DataItem> promoted)
    {
        if (!visited.Add(item)) return;          // reference dedup — LINKAGE roots are already in Roots; temps share a Pic
        if (item.IsElementary) item.Storage = Classify(item, promoted);
        foreach (var c in item.Children) Walk(c, visited, promoted);
    }

    /// <summary>The base + image-promoted <see cref="StorageForm"/> of one ELEMENTARY item.</summary>
    private static StorageForm Classify(DataItem item, HashSet<DataItem> promoted)
    {
        // (1) OCCURS DYNAMIC — the out-of-line table wraps its element's own form (highest priority; §8.5.1.9.1, D9).
        // ⛔ THE ELEMENT IS PROMOTED BY THE SAME RULE AS ANY LEAF (kb/Work PB1939, PB2004). An occurrence receives
        // characters exactly as a fixed-table occurrence does — §14.6.9.4's space fill ("each element of the
        // dynamic table is space-filled"), a group MOVE's characters (§14.9.25.4 GR4), INSPECT's retained sign
        // over a zero magnitude (§14.9.22.4 GR4 d), a negative zero) — and a native carrier holds a VALUE, so
        // the element whose item a channel writes stores its image: CobolDynTable<string>, not <long>.
        if (item.IsDynamicTable) return new StorageForm.DynamicTable(Promoted(item, BaseElementary(item), promoted));

        // (1b) DYNAMIC LENGTH — a variable-length, min-0 native string (§8.5.1.10 / §13.18.19, COBOL-2014). The
        // category (X→Alphanumeric, N→National) comes from the PIC; the limit is the LIMIT phrase (-1 = implementor max).
        if (item.IsDynamicLength)
            return new StorageForm.DynamicString(item.Pic?.Category ?? PicCategory.Alphanumeric, item.DynMaxSize);

        // (2) A REDEFINES view member (non-canonical member of a class).
        if (item.Class is { } cls && !item.IsCanonical)
        {
            if (cls.Tier == RedefinesTier.StringCanonical)
                // A Tier-B numeric-DISPLAY / BINARY / PACKED leaf is image-stored (the ClassifyRedefinesClasses
                // fact record) → CharImage(Numeric); a Tier-B group backing or non-numeric-display view → the
                // typed (offset, width) window. A Ptr-FORCED StringCanonical class records no facts — its
                // display leaves stay windows, exactly like the legacy flag.
                return promoted.Contains(item)
                    ? new StorageForm.CharImage(item.ImageWidth, PicCategory.Numeric)
                    : new StorageForm.TierBWindow(cls, item.ClassOffset, item.ImageWidth);
            if (cls.Tier == RedefinesTier.Alias)
                return Classify(cls.Canonical, promoted);   // Tier-A: forward to the canonical's form
            // Rejected (Tier-D backstop / NATIONAL residue): fall through to base classify — the reference
            // resolver already refuses non-canonical members of a Rejected class (the runtime loud).
        }

        // (3) Base classification, then the numeric-image promotion for a whole-group / figurative / ref-mod /
        //     file-record / print-item leaf (the OO harmonize applies its flips AFTER classification, at the
        //     Storage level).
        return Promoted(item, BaseElementary(item), promoted);
    }

    /// <summary>The numeric-image promotion of one leaf's (or one dynamic-table element's) base form.
    /// ⛔ The promotion admits every byte-form NATIVE shape, not only NativeInt (the Step D design scout's
    /// canonical-vs-view asymmetry: arm 2 of <see cref="Classify"/> promotes ANY promoted view to CharImage,
    /// while the canonical-side arm promoted only NativeInt — a CANONICAL float/INDEX member of a widened Tier-B
    /// class kept its native form while its views were image-stored, splitting one storage in two).
    /// The predicate is the ONE image predicate: a promoted leaf whose Pic carries a pinned byte form.</summary>
    private static StorageForm Promoted(DataItem item, StorageForm form, HashSet<DataItem> promoted) =>
        promoted.Contains(item) && form is StorageForm.NativeInt or StorageForm.NativeFloat or StorageForm.IndexCell
            ? new StorageForm.CharImage(item.ImageWidth, PicCategory.Numeric)
            : form;

    /// <summary>The pre-promotion base form from (Pic.Category, Usage, tier, dynamic). <c>item.ImageWidth</c> is the
    /// character-image width (= <c>ElementaryImageWidth</c>: digits + separate-sign, or PIC length) — the source of
    /// truth this pass reproduces.</summary>
    private static StorageForm BaseElementary(DataItem item)
    {
        if (item.Pic is not { } pic) return new StorageForm.CharImage(0, PicCategory.Alphanumeric);   // Pic-null recovery leaf
        return pic.Category switch
        {
            PicCategory.ObjectReference => new StorageForm.ObjectRef(pic.ObjectRef ?? ObjectRefDescriptor.Universal),
            PicCategory.Pointer => new StorageForm.PointerRef(),
            PicCategory.ProgramPointer => new StorageForm.ProgramPointerRef(),
            PicCategory.FunctionPointer => new StorageForm.FunctionPointerRef(),
            PicCategory.Numeric when pic.Usage == Usage.Index => new StorageForm.IndexCell(item.ImageWidth),
            PicCategory.Numeric when pic.IsFloat => new StorageForm.NativeFloat(pic.IsSingle, item.ImageWidth),
            PicCategory.Numeric => new StorageForm.NativeInt(pic.IsWide, pic.Digits, item.ImageWidth),
            // Alphanumeric / NumericEdited / National / Boolean — the base string categories.
            _ => new StorageForm.CharImage(item.ImageWidth, pic.Category),
        };
    }

    // ── The item-level facts DERIVED from Storage (what Verify compares against the legacy recursive props) ──

    /// <summary>Reproduces <c>DataItem.IsCharacterImage</c> off <see cref="StorageForm"/> instead of the string-category
    /// test + <c>StoreAsImage</c>. Leaf = its form's flag; group = every child is character-image; dynamic = no.</summary>
    public static bool IsCharacterImageOf(DataItem item) =>
        !item.IsDynamicTable && (item.IsElementary
            ? item.Storage!.IsCharacterImage
            : item.IsGroup && item.Children.All(IsCharacterImageOf));

    /// <summary>Reproduces <c>DataItem.ElementType</c> off <see cref="StorageForm"/> (a group is its record-struct
    /// name; an elementary is its form's CLR type — a promoted numeric leaf is "string", not its Pic.ClrType).</summary>
    public static string ElementTypeOf(DataItem item) =>
        item.IsGroup ? item.StructName : StorageElementType(item.Storage!);

    private static string StorageElementType(StorageForm f) => f switch
    {
        StorageForm.CharImage => "string",
        StorageForm.NativeInt ni => ni.Wide ? "Int128" : "long",
        StorageForm.NativeFloat nf => nf.Single ? "float" : "double",
        StorageForm.IndexCell => "long",
        StorageForm.ObjectRef o => o.Desc.ClrTypeName + "?",   // ONE derivation, shared with PicInfo.ClrType (PB389)
        StorageForm.PointerRef => "ManagedPointer",
        StorageForm.ProgramPointerRef => "ProgramPointer",
        StorageForm.FunctionPointerRef => "FunctionPointer",
        StorageForm.TierBWindow => "string",       // a Tier-B window is a string slice (a numeric Tier-B leaf is CharImage)
        StorageForm.DynamicTable dt => StorageElementType(dt.Element),
        StorageForm.DynamicString => "string",      // a DYNAMIC LENGTH item IS a native string (§8.5.1.10)
        _ => "object",
    };

    /// <summary>The corpus-wide PROVE assert (DESIGN Phase D0 / exit criterion 3): for every item under a binder, the
    /// StorageForm identities (#1–#4) between the new <see cref="StorageForm"/> and the legacy computation, PLUS the
    /// PHASE-05 §2.6 <see cref="RecordLayout"/> physical-width identity (#5). Returns the list of divergences (empty =
    /// proven equal). #1 is tautological in D0 (Storage is promoted FROM StoreAsImage) — it is the guard for the later
    /// delete phase; the value here is #2/#3/#4 (the derived recursive facts still hold once Storage, not the scattered
    /// flag, is the source) and #5 (the RecordLayout width authority reproduces the legacy tier-aware physical extent —
    /// the Step-9 drift guard).</summary>
    public static List<string> Verify(DataBinder data)
    {
        var d = new List<string>();
        var visited = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        void Check(DataItem item)
        {
            if (!visited.Add(item)) return;
            if (item.IsElementary)
            {
                if (item.Storage is null) { d.Add($"NO-STORAGE: {Desc(item)}"); return; }
                // (identity #1 — Storage-promotion == StoreAsImage — RETIRED at P5.7b: the flag is a projection
                //  OF Storage now, so the comparison is tautological; its one real run was the P5.7a parity gate.)
                if (ElementTypeOf(item) != item.ElementType)
                    d.Add($"#4 ElementType: {Desc(item)} derived={ElementTypeOf(item)} legacy={item.ElementType}");
            }
            if (IsCharacterImageOf(item) != item.IsCharacterImage)
                d.Add($"#2 IsCharacterImage: {Desc(item)} derived={IsCharacterImageOf(item)} legacy={item.IsCharacterImage}");
            // (identity #3 — RecordLayout.ImageWidth == DataItem.ImageWidth — RETIRED at kb/Work PB1665: the copy
            //  is deleted, DataItem.ImageWidth IS RecordLayout.ImageWidth, so the comparison became tautological.)
            // #5 (PHASE-05 Step 4): the RecordLayout §2.6 width authority reproduces the legacy tier-aware physical
            // (identity #5 — RecordLayout.PhysicalWidth == OdoModel.PhysicalWidth — RETIRED at P5.9: the last
            //  duplicate copy is deleted; RecordLayout is the single width/offset authority.)
            foreach (var c in item.Children) Check(c);
        }
        foreach (var root in data.Roots) Check(root);
        foreach (var report in data.Reports)
            foreach (var group in report.Groups)
                foreach (var line in group.Lines)
                    foreach (var field in line.Fields)
                    Check(field.PrintItem);
        return d;
    }

    private static string Desc(DataItem item) =>
        $"{item.CobolName ?? "FILLER"}#{item.Uid} (pic={item.Pic?.Category.ToString() ?? "group"}/{item.Pic?.Usage.ToString() ?? "-"})";
}
