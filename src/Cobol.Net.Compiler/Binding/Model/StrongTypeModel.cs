// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// The strong-typing model over the <see cref="DataItem"/> tree (ISO/IEC 1989:2023 §8.5.3 / §13.18.58 STRONG
/// TYPEDEF; data-model D17) — §8.5.3.1's SAME-TYPE test and the tree-walking predicates behind the MOVE /
/// relation / class-condition / argument / RETURNING / REDEFINES / RENAMES gates. Extracted off
/// <c>DataItem</c> (P5.11b, DESIGN-data-model §2.4): strong typing is a USE-RESTRICTION overlay consulted at a
/// handful of check sites, not core record shape — <c>DataItem</c> keeps only the stored facts
/// (<see cref="DataItem.StrongType"/>, <see cref="DataItem.TypeName"/>) that <c>ExpandTypes</c> writes.
/// <para>⛔ THERE IS ONE "same type" IN THE STANDARD AND ONE PREDICATE HERE. §8.5.3.1 defines it, and eight
/// separate rules spend it — §14.9.25.3 SR2 (MOVE), §8.8.4.2.3 SR1 + §8.8.4.2.12 (comparison), §14.8.2.2
/// (arguments), §14.8.3.2 (returning items), §9.3.8.2.3 rule 7 (interface conformance), §8.5.1.12.1's
/// fixed-length sentence, §14.9.30.3 SR2 / §14.9.34.3 SR3 (the record area). Every one of them asks
/// <see cref="SameType"/>; none of them re-derives the test (kb/Work PB427 — the predicate used to compare a
/// type-NAME string plus a member-NAME PATH, which is neither of §8.5.3.1's two halves and was wrong in BOTH
/// directions).</para>
/// </summary>
public static class StrongTypeModel
{
    /// <summary>The outermost enclosing item (the item itself or an ancestor) whose data description is strongly
    /// typed — i.e. carries, or is subordinate to, a TYPE clause referencing a STRONG type declaration (ISO
    /// §8.5.3.1). Null when the item is not part of any strongly-typed subtree. Backs the §8.5.3.3
    /// use-restriction gates and the §8.5.3.1 same-type test.</summary>
    public static DataItem? StrongRoot(DataItem item)
    {
        DataItem? root = null;
        for (DataItem? cur = item; cur is not null; cur = cur.Parent)
            if (cur.StrongType) root = cur;
        return root;
    }

    /// <summary>True when the item is a strongly-typed GROUP — the operand form the MOVE / comparison /
    /// class-condition same-type gates restrict (ISO §8.5.3.3: only group items may be strongly typed;
    /// §14.9.25.3 SR2 / §8.8.4.2.3 SR1 / §8.8.4.4.3 SR1). An elementary leaf subordinate to a strong group is
    /// NOT strongly typed, so its individual MOVE / comparison is unrestricted (a strong record is still built
    /// up field by field).</summary>
    public static bool IsStrongGroup(DataItem item) => item.IsGroup && StrongRoot(item) is not null;

    /// <summary>True when the item is part of any strongly-typed subtree (a strong group OR a leaf subordinate
    /// to one) — backs the §13.18.57.3 SR3/SR4 "in whole or in part" declaration checks (a RENAMES / REDEFINES
    /// touching any part of a strong item is prohibited).</summary>
    public static bool IsStronglyTyped(DataItem item) => StrongRoot(item) is not null;

    /// <summary>The NEAREST enclosing item (the item itself or an ancestor) that directly carries a TYPE clause
    /// — the item whose <see cref="DataItem.TypeName"/> it acquired. This is the item's TYPE DECLARATION for the
    /// §8.5.3.1 same-type test: a nested <c>TYPE INNER-T</c> subgroup is anchored by INNER-T (itself), NOT by the
    /// outermost strong record. Null when the item is not part of any typed subtree.</summary>
    public static DataItem? TypeAnchor(DataItem item)
    {
        for (DataItem? cur = item; cur is not null; cur = cur.Parent)
            if (cur.TypeName is not null) return cur;
        return null;
    }

    // ── §8.5.3.1 — THE SAME-TYPE TEST ─────────────────────────────────────────────────────────────────────────
    //
    // The standard states it as TWO alternatives over typed ITEMS, both resting on one relation over type
    // DECLARATIONS:
    //
    //   "Two typed items are of the same type when:
    //    — The items are described with TYPE clauses that reference equivalent type declarations; or
    //    — The items are described as subordinate items in equivalent type declarations, starting at the same
    //      relative byte pr bit position and having the same length in bytes or bits."
    //                                        ^ the transcription's OCR slip for "or"; quoted verbatim so the
    //                                          `scripts/spec/cite.py --check` of this line is honest.
    //
    //   "Two type declarations are considered equivalent when they have the same type-name, both have the same
    //    presence or absence of the EXTERNAL clause and the STRONG phrase, and for each elementary item in one
    //    type declaration there is a corresponding elementary item in the other type declaration, starting at
    //    the same relative byte or bit position and having the same length in bytes or bits. Each pair of
    //    corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED,
    //    PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses …"
    //
    // ⛔ A TYPE-NAME MATCH IS ONE CONJUNCT OF ONE HALF. It is not the test, and neither is a member-NAME path:
    // §8.5.3.1's own NOTE ("either both items are described with the same TYPE clause or both items are
    // described as the same subordinate item in the same type declaration") describes what the rules MEAN
    // within ONE source element — it is informative, it is not the criterion, and reading it as one both
    // ADMITTED a MOVE between two non-equivalent same-named declarations in different source elements
    // (alphabetic characters deposited into two PIC 9(3) items, silently) and REFUSED a legal move between two
    // differently-NAMED subgroups at the same relative position (kb/Work PB427).

    /// <summary>ISO §8.5.3.1 — "Two typed items are of the same type when …". The ONE same-type predicate:
    /// every rule in the standard that says "of the same type" of two typed items asks this.
    /// <para>Which of the clause's two alternatives applies is decided by whether the operand IS its own
    /// <see cref="TypeAnchor"/>: an item that carries a TYPE clause is "described with a TYPE clause"
    /// (alternative 1 — equivalent declarations, nothing more); an item that does not is "described as a
    /// subordinate item in a type declaration" (alternative 2 — equivalent declarations PLUS the same relative
    /// position and length within them). A MIXED pair — one whole typed item against another's subordinate — is
    /// covered by NEITHER alternative and is therefore not of the same type.</para></summary>
    public static bool SameType(DataItem a, DataItem b)
    {
        if (TypeAnchor(a) is not { } ra || TypeAnchor(b) is not { } rb) return false;
        bool aIsSubject = ReferenceEquals(ra, a), bIsSubject = ReferenceEquals(rb, b);
        if (aIsSubject != bIsSubject) return false;
        if (!EquivalentTypeDeclarations(ra, rb)) return false;
        if (aIsSubject) return true;                                     // alternative 1
        // Alternative 2 — "starting at the same relative byte or bit position and having the same length in
        // bytes or bits". BITS is the unit that expresses both (§8.5.1.6.3 places bit items at bit positions),
        // and the placement walk is BitLayout's ONE §8.5.1.6.3 cursor — never a second geometry here.
        int pa = BitLayout.StartBitOf(ra, a), pb = BitLayout.StartBitOf(rb, b);
        return pa >= 0 && pa == pb && BitLayout.RunBits(a) == BitLayout.RunBits(b);
    }

    /// <summary>ISO §8.5.3.1 — "Two type declarations are considered equivalent when …". Takes either form the
    /// model has of a declaration: a TYPE-clause SUBJECT (whose subtree <c>DataBinder.ExpandType</c> cloned
    /// from the template through the ONE <c>CopyEntryDescription</c>, so it IS the declaration, kb/Work PB522)
    /// or the TYPEDEF TEMPLATE itself (<c>DataBinder.TypeDecls</c>' entry — also fully expanded, because
    /// <c>ExpandTypes</c> walks the templates too). <see cref="DeclaredTypeName"/> /
    /// <see cref="DeclaredStrong"/> / <see cref="DeclaredExternal"/> read the three level-1 identity facts from
    /// whichever form it is, so a restricted data-pointer's resolved declaration and a TYPE subject's anchor
    /// compare through the same walk.</summary>
    public static bool EquivalentTypeDeclarations(DataItem a, DataItem b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (!CobolNames.Same(DeclaredTypeName(a), DeclaredTypeName(b))
            || DeclaredTypeName(a) is null)
            return false;
        if (DeclaredStrong(a) != DeclaredStrong(b) || DeclaredExternal(a) != DeclaredExternal(b)) return false;
        return SameElementaryLayout(a, b);
    }

    /// <summary>ISO §8.5.3.1's STRUCTURAL half — "for each elementary item in one type declaration there is a
    /// corresponding elementary item in the other type declaration, starting at the same relative byte or bit
    /// position and having the same length in bytes or bits", each pair with the same ALIGNED, BLANK WHEN ZERO,
    /// DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED and USAGE clauses — asked of two data description
    /// entries whatever their type identity. <see cref="EquivalentTypeDeclarations"/> is this plus the type-name,
    /// STRONG and EXTERNAL conjuncts; the prototype-versus-definition signature compare
    /// (<see cref="PrototypeSignatures.Same"/>, ISO §13.7.3 SR2 / §10.6.2 SR2–SR3, kb/Work PB1115) asks it of two
    /// untyped group formals, because a group's description IS its subordinate entries' descriptions.
    /// <para>The correspondence IS position-and-length, and §13.18.44.3 SR12/SR14 keep REDEFINES out of a
    /// strongly-typed declaration, so the elementary items of an equivalent pair tile their declaration
    /// identically and declaration order IS position order — the two sequences are walked pairwise. GROUPING is
    /// deliberately NOT compared: §8.5.3.1's first paragraph makes the essential characteristics "the relative
    /// positions and lengths of the ELEMENTARY items … and the ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH,
    /// JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses specified for each of these elementary items",
    /// and an intermediate group level is none of those.</para></summary>
    public static bool SameElementaryLayout(DataItem a, DataItem b)
    {
        List<(DataItem Item, int Start)> la = [], lb = [];
        if (!CollectElementary(a, 0, la) || !CollectElementary(b, 0, lb) || la.Count != lb.Count) return false;
        for (int i = 0; i < la.Count; i++)
            if (la[i].Start != lb[i].Start
                || BitLayout.RunBits(la[i].Item) != BitLayout.RunBits(lb[i].Item)
                || !SameEssentialCharacteristics(la[i].Item, lb[i].Item))
                return false;
        return true;
    }

    /// <summary>The type-name of a declaration in either of its two forms — a TYPEDEF template's own name
    /// (§13.18.58: the TYPEDEF clause "declares and names" the type) or the name a TYPE-clause subject
    /// references (<see cref="DataItem.TypeName"/>). Null when the item is neither.</summary>
    private static string? DeclaredTypeName(DataItem d) => d.IsTypedef ? d.CobolName : d.TypeName;

    /// <summary>"…both have the same presence or absence of … the STRONG phrase" (§8.5.3.1) — the template's own
    /// STRONG phrase, or the strength a TYPE subject acquired from the template it references.</summary>
    private static bool DeclaredStrong(DataItem d) => d.IsTypedef ? d.TypedefStrong : d.StrongType;

    /// <summary>"…both have the same presence or absence of the EXTERNAL clause …" AT LEVEL 1 OF THE TYPE
    /// DECLARATION (§8.5.3.1 ¶1) — so the fact read is the TYPE DECLARATION's own EXTERNAL clause
    /// (<see cref="DataItem.IsExternalTypedef"/>), never the referencing record's
    /// (<see cref="DataItem.HasExternalClause"/>). On a TYPE subject the carrier is
    /// <see cref="DataItem.ExternalFromType"/>, which <c>ExpandType</c> writes under §13.18.22.4 GR3; GR2
    /// confines such a subject to level 1, and a non-level-1 one is already a hard error there, so for every
    /// conforming program this reads the declaration's clause exactly.</summary>
    private static bool DeclaredExternal(DataItem d) => d.IsTypedef ? d.IsExternalTypedef : d.ExternalFromType;

    /// <summary>The declaration's elementary items with their relative BIT positions, in declaration order.
    /// The placement is composed out of <see cref="BitLayout.StartBitWithin"/> — the ONE §8.5.1.6.3 cursor walk
    /// — level by level, exactly as <see cref="BitLayout.StartBitOf"/> composes it, so no second geometry is
    /// written here and a bit-bearing declaration is measured by the same law as its storage. False when any
    /// level cannot be placed (an unmodelled overlay chain): a declaration whose geometry is unknown is never
    /// declared equivalent to another.</summary>
    private static bool CollectElementary(DataItem node, int at, List<(DataItem Item, int Start)> into)
    {
        if (node.Children.Count == 0) { into.Add((node, at)); return true; }
        foreach (var c in node.Children)
        {
            int off = BitLayout.StartBitWithin(node, c);
            if (off < 0 || !CollectElementary(c, at + off, into)) return false;
        }
        return true;
    }

    /// <summary>ISO §8.5.3.1 — "Each pair of corresponding elementary items shall have the same ALIGNED, BLANK
    /// WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses". The clause's own
    /// eight-item list, one conjunct each; the PICTURE / SIGN / USAGE trio is answered by the analyzed profile
    /// (<see cref="PicInfo"/> IS the canonical PICTURE + USAGE + SIGN analysis).</summary>
    private static bool SameEssentialCharacteristics(DataItem x, DataItem y) =>
        DescriptionClauses.AlignedOrDynamicLengthMismatch(x, y) is null       // ALIGNED §13.18.1; DYNAMIC LENGTH
                                                                              //   §13.18.19 with LIMIT + structure (PB829)
        && x.BlankWhenZero == y.BlankWhenZero                                 // BLANK WHEN ZERO §13.18.8
        && x.Justified == y.Justified                                         // JUSTIFIED      §13.18.32
        && x.Synchronized == y.Synchronized                                   // SYNCHRONIZED   §13.18.55
        && SameAnalyzedProfile(x.Pic, y.Pic);                                 // PICTURE + SIGN + USAGE

    /// <summary>The PICTURE / SIGN / USAGE conjunct, with §8.5.3.1's two exceptions and its "Additionally"
    /// paragraph.
    /// <para>The comparison is <see cref="PicInfo"/>'s OWN record equality — every analyzed axis, and every axis
    /// a later slice adds, is included by construction rather than by a hand-maintained member list. Three
    /// members are handled apart from it:</para>
    /// <list type="bullet">
    ///   <item><b>Exceptions 1 and 2</b> — "Currency symbols match if and only if the corresponding currency
    ///   strings are the same", and the period / comma picture symbols match only when DECIMAL-POINT IS COMMA is
    ///   in effect for both declarations or for neither. Both are carried by <see cref="PicInfo.Clause"/> — the
    ///   ONE PICTURE-clause identity (<see cref="PictureClauseIdentity"/>, kb/Work PB1166) that the §14.8 / §9.3
    ///   activation and signature comparator reads too — which holds the canonical character-string, the currency
    ///   STRING, and the DECIMAL-POINT IS COMMA state whenever the picture has a period or comma symbol. This used
    ///   to rest on the claim that two items analyzed under different settings "differ in the profile already"
    ///   through <see cref="PicInfo.Scale"/>, which holds only when a digit position follows the separator — the
    ///   rule is about the SYMBOLS, so it is now carried as the symbols' own fact.</item>
    ///   <item><b>The "Additionally" paragraph</b> — "locale specifications in the PICTURE clauses match if and
    ///   only if" both specify the same SIZE phrase and the same external identification — never the
    ///   locale-NAME, so two locale-names bound to one identification match (kb/Work PB1475). Carried on
    ///   <see cref="PicInfo.LocaleEdit"/>, whose <see cref="LocaleEditSpec"/> equality IS that rule
    ///   (<see cref="LocaleRef.SameIdentificationAs"/>), the predicate the OoConformance arms read too.</item>
    /// </list>
    /// <para>⛔ <see cref="PicInfo.EditingRules"/> is an <c>IReadOnlyList</c>, for which the record's synthesized
    /// equality uses the DEFAULT comparer — reference equality — so two separately-analyzed items with identical
    /// PICTURE EDITING phrases would compare unequal and legal source would be refused. It is elided from the
    /// record compare and compared element-wise. <see cref="PicInfo.RestrictedTypeDecl"/> is elided too: it is
    /// the RESOLUTION of <see cref="PicInfo.RestrictedTypeName"/>, which the record compare already covers, and
    /// recursing into it would not terminate for a type whose pointer member is restricted back to it.</para>
    /// </summary>
    private static bool SameAnalyzedProfile(PicInfo? x, PicInfo? y)
    {
        if (x is null || y is null) return x is null && y is null;
        if (ReferenceEquals(x, y)) return true;
        if ((x with { EditingRules = null, RestrictedTypeDecl = null })
            != (y with { EditingRules = null, RestrictedTypeDecl = null })) return false;
        return x.EditingRules is null ? y.EditingRules is null
            : y.EditingRules is { } yr && x.EditingRules.SequenceEqual(yr);
    }

    // ── RESTRICTED DATA-POINTERS (ISO Annex D.9.2.2; §13.18.60.4 GR23, §8.4.3.11.4 GR2) ─────────────────────
    // Annex D.9.2.2 names the model's TWO sources outright: a restricted data-pointer arises "1) by specifying a
    // data description entry that contains a usage clause of the form USAGE POINTER TO type-name-1, or 2) by
    // specifying a data-address-identifier (ADDRESS OF identifier-1, where identifier-1 is a strongly typed group
    // item or another restricted data-pointer)". The SECOND source needs no grammar at all and was violable the
    // day strong TYPEDEF landed — the whole strong-type use-restriction network was built out across MOVE / CALL /
    // ACCEPT / STRING / REDEFINES / RENAMES / intrinsics and the POINTER subsystem was never wired in (kb/Work
    // PB153). Both sources are reduced HERE to one thing — a TYPE IDENTITY — so the consumption screens
    // (§14.9.3.3 SR4/SR5, §14.9.39.3 SR19, §14.8.2.3.2) all ask the same question of the same model.

    /// <summary>The identity of the TYPE a value is restricted to: the type-NAME the restriction is spelled with
    /// (§13.18.60.4 GR23 states it as <c>type-name-1</c>) together with the type DECLARATION that name resolves
    /// to. <see langword="default"/> — a null <see cref="Name"/> — is UNRESTRICTED.
    /// <para>Both are carried because a restriction's spelling and its declaration answer different halves of
    /// §8.5.3.1: the name is the first conjunct of declaration equivalence, and the declaration is the rest of
    /// it. Within one source element a type-name resolves to exactly one declaration (§13.18.58 makes the
    /// type-name unique), so the name alone decides; ACROSS source elements two declarations may share a name
    /// and differ — which is why the declaration travels with it.</para></summary>
    /// <param name="Name">The type-name the restriction names, or null when unrestricted.</param>
    /// <param name="Declaration">The type declaration the name resolves to, when the model has it: a TYPE
    /// subject's anchor, or the TYPEDEF template <c>DataBinder.ResolveRestrictedTypes</c> attached. Null when
    /// only the name is known.</param>
    public readonly record struct TypeRestriction(string? Name, DataItem? Declaration)
    {
        /// <summary>True when this is a restriction at all (Annex D.9.2.2 — an ordinary data-pointer is not).</summary>
        public bool IsRestricted => Name is not null;

        /// <inheritdoc/>
        public override string ToString() => Name ?? "(unrestricted)";
    }

    /// <summary>The type a DATA-POINTER ITEM's value is restricted to — Annex D.9.2.2 source 1, the declared
    /// <c>USAGE POINTER TO type-name-1</c> (§13.18.60.4 GR23). <see langword="default"/> for an ordinary
    /// data-pointer, and for any item that is not a data-pointer at all.</summary>
    public static TypeRestriction PointerRestriction(DataItem item) =>
        item.Pic is { Category: PicCategory.Pointer } pic && pic.RestrictedTypeName is not null
            ? new TypeRestriction(pic.RestrictedTypeName, pic.RestrictedTypeDecl)
            : default;

    /// <summary>The type an <c>ADDRESS OF identifier-1</c> VALUE is restricted to — Annex D.9.2.2 source 2, whose
    /// normative statement is §8.4.3.11.4 GR2: "If identifier-1 is a strongly-typed group item or a restricted
    /// data-pointer, the data-address-identifier is a restricted data-pointer that is restricted to the type of
    /// identifier-1." <see langword="default"/> when the operand is neither, i.e. the address is unrestricted.
    /// <para>⛔ The strong-group arm deliberately uses <see cref="IsStrongGroup"/>, so a LEAF subordinate to a
    /// strong group yields no restriction: <c>SET P TO ADDRESS OF &lt;leaf of a strong record&gt;</c> is
    /// UNRESTRICTED and must stay legal. That asymmetry is the model's own (a strong record is still built up
    /// field by field), and an over-eager screen here would break it.</para></summary>
    public static TypeRestriction AddressOfRestriction(DataItem operand) =>
        StrongGroupType(operand) is { IsRestricted: true } r ? r : PointerRestriction(operand);

    /// <summary>"Restricted to the same type" (§14.9.39.3 SR19, §14.8.2.3.2, §14.9.3.3 SR4/SR5) — the same
    /// §8.5.3.1 relation every other same-type rule spends, asked of the identity a restriction carries: the
    /// type-name always, and full declaration equivalence whenever BOTH sides resolved to a declaration. A
    /// restriction is to a TYPE, not to a member position, so this is declaration equivalence and NOT
    /// <see cref="SameType"/>, whose second alternative answers the different question of whether two OPERANDS
    /// occupy corresponding positions within their declarations.</summary>
    public static bool SameRestriction(TypeRestriction a, TypeRestriction b) =>
        a.IsRestricted && b.IsRestricted
        && CobolNames.Same(a.Name, b.Name)
        && (a.Declaration is null || b.Declaration is null
            || EquivalentTypeDeclarations(a.Declaration, b.Declaration));

    /// <summary>The type a TYPED data item is of — §14.9.3.3 SR4's "shall reference a typed data item, and the
    /// data item referenced by data-name-2 shall be restricted to the type of data-name-1", whose antecedent is
    /// TYPED, strongly or weakly. <see langword="default"/> when the item is untyped. The declaration travels
    /// with the name: the anchor's own subtree IS the type declaration.</summary>
    public static TypeRestriction TypedItemType(DataItem item) =>
        TypeAnchor(item) is { } anchor ? new TypeRestriction(anchor.TypeName, anchor) : default;

    /// <summary>The type a strongly-typed group item IS (§14.9.3.3 SR5's "the type of data-name-1"), or
    /// <see langword="default"/> when the item is not a strongly-typed group. ⛔ SR4 and SR5 ask about
    /// DIFFERENT things — SR5's antecedent is a STRONGLY-TYPED GROUP, SR4's only a TYPED item — so the two keep
    /// distinct accessors (this one and <see cref="TypedItemType"/>); conflating them rejects legal source.</summary>
    public static TypeRestriction StrongGroupType(DataItem item) =>
        IsStrongGroup(item) ? TypedItemType(item) : default;
}
